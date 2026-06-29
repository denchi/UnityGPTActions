using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace GPTUnity.Actions
{
    [GPTAction("Checks which high-value Unity profiler counters are available in the current editor session so later optimization probes can use the best counters automatically.", Name = "probe_profiler_counter_support")]
    public class ProbeProfilerCounterSupportAction : GPTAssistantAction
    {
        [GPTParameter("Optional category filter such as 'Render', 'Memory', or 'Internal'. Leave empty to inspect the curated default counter set.")]
        public string CategoryFilter { get; set; }

        public override async Task<string> Execute()
        {
            var categories = ParseCategories(CategoryFilter);
            var results = ProfileCounterCatalog.CreateCuratedChecks()
                .Where(spec => categories.Count == 0 || categories.Contains(spec.Category))
                .Select(ProfileCounterCatalog.ProbeCounter)
                .ToList();

            return JsonConvert.SerializeObject(new
            {
                inspectedAtUtc = DateTime.UtcNow,
                inspectedCategories = categories.Count == 0 ? new[] { "All curated categories" } : categories.Select(c => c.Name).ToArray(),
                counters = results,
                availableCount = results.Count(result => result.available),
                unavailableCount = results.Count(result => !result.available)
            }, Formatting.Indented);
        }

        private static HashSet<ProfilerCategory> ParseCategories(string raw)
        {
            var categories = new HashSet<ProfilerCategory>();
            if (string.IsNullOrWhiteSpace(raw))
                return categories;

            var chunks = raw.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var chunk in chunks)
            {
                if (ProfileCounterCatalog.TryGetCategory(chunk.Trim(), out var category))
                {
                    categories.Add(category);
                }
            }

            return categories;
        }
    }

    [GPTAction("Samples lightweight frame-budget metrics over editor updates or play mode frames and returns structured timing and rendering counters for quick performance checks.", Name = "probe_frame_budget")]
    public class ProbeFrameBudgetAction : GPTAssistantAction
    {
        [GPTParameter("Number of sampled updates or frames to collect after warmup.", Name = "sample_count")]
        public int SampleCount { get; set; } = 120;

        [GPTParameter("Number of initial updates or frames to ignore before recording metrics.", Name = "warmup_count")]
        public int WarmupCount { get; set; } = 20;

        [GPTParameter("Require play mode before collecting gameplay frame metrics. Disable this to sample editor updates instead.", Name = "require_play_mode")]
        public bool RequirePlayMode { get; set; } = true;

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            if (RequirePlayMode && !EditorApplication.isPlaying)
                return "Enter Play Mode or call probe_frame_budget with require_play_mode=false.";

            var sampleCount = Mathf.Clamp(SampleCount, 10, 2000);
            var warmupCount = Mathf.Clamp(WarmupCount, 0, 500);
            var sample = await CollectFrameProbeAsync(sampleCount, warmupCount);
            return JsonConvert.SerializeObject(sample, Formatting.Indented);
#else
            return "This action can only be run in the Unity Editor.";
#endif
        }

#if UNITY_EDITOR
        private static Task<object> CollectFrameProbeAsync(int sampleCount, int warmupCount)
        {
            var tcs = new TaskCompletionSource<object>();
            var updateIntervalsMs = new List<double>(sampleCount);
            var playModeDeltaMs = new List<double>(sampleCount);
            var cpuFrameMs = new List<double>(sampleCount);
            var gpuFrameMs = new List<double>(sampleCount);
            var startedAtUtc = DateTime.UtcNow;
            var sampleIndex = -warmupCount;
            var previousUpdateTime = EditorApplication.timeSinceStartup;
            var mainThread = ProfileCounterCatalog.StartCounter(ProfileCounterCatalog.MainThreadTime);
            var renderThread = ProfileCounterCatalog.StartCounter(ProfileCounterCatalog.RenderThreadTime);
            var drawCalls = ProfileCounterCatalog.StartCounter(ProfileCounterCatalog.DrawCalls);
            var batches = ProfileCounterCatalog.StartCounter(ProfileCounterCatalog.Batches);
            var setPassCalls = ProfileCounterCatalog.StartCounter(ProfileCounterCatalog.SetPassCalls);
            var triangles = ProfileCounterCatalog.StartCounter(ProfileCounterCatalog.Triangles);
            var vertices = ProfileCounterCatalog.StartCounter(ProfileCounterCatalog.Vertices);
            var counters = new[]
            {
                mainThread,
                renderThread,
                drawCalls,
                batches,
                setPassCalls,
                triangles,
                vertices
            };

            void Cleanup()
            {
                EditorApplication.update -= OnUpdate;
                foreach (var counter in counters)
                    counter.Dispose();
            }

            void Finish()
            {
                Cleanup();
                tcs.TrySetResult(new
                {
                    startedAtUtc,
                    finishedAtUtc = DateTime.UtcNow,
                    playMode = EditorApplication.isPlaying,
                    sampleCount = updateIntervalsMs.Count,
                    warmupCount,
                    updateIntervalMs = BuildDistribution(updateIntervalsMs),
                    playModeDeltaMs = playModeDeltaMs.Count > 0 ? BuildDistribution(playModeDeltaMs) : null,
                    cpuFrameTimingMs = cpuFrameMs.Count > 0 ? BuildDistribution(cpuFrameMs) : null,
                    gpuFrameTimingMs = gpuFrameMs.Count > 0 ? BuildDistribution(gpuFrameMs) : null,
                    counters = new
                    {
                        mainThreadMs = mainThread.ToSummaryNsAsMs(),
                        renderThreadMs = renderThread.ToSummaryNsAsMs(),
                        drawCalls = drawCalls.ToSummaryRaw(),
                        batches = batches.ToSummaryRaw(),
                        setPassCalls = setPassCalls.ToSummaryRaw(),
                        triangles = triangles.ToSummaryRaw(),
                        vertices = vertices.ToSummaryRaw()
                    }
                });
            }

            void OnUpdate()
            {
                try
                {
                    var now = EditorApplication.timeSinceStartup;
                    var updateInterval = Math.Max(0d, (now - previousUpdateTime) * 1000d);
                    previousUpdateTime = now;
                    sampleIndex++;

                    FrameTimingManager.CaptureFrameTimings();
                    if (sampleIndex < 0)
                        return;

                    updateIntervalsMs.Add(updateInterval);

                    if (Application.isPlaying)
                    {
                        var deltaMs = Time.unscaledDeltaTime * 1000f;
                        if (deltaMs > 0f)
                            playModeDeltaMs.Add(deltaMs);
                    }

                    var frameTimings = new FrameTiming[1];
                    if (FrameTimingManager.GetLatestTimings(1, frameTimings) > 0)
                    {
                        if (frameTimings[0].cpuFrameTime > 0d)
                            cpuFrameMs.Add(frameTimings[0].cpuFrameTime);
                        if (frameTimings[0].gpuFrameTime > 0d)
                            gpuFrameMs.Add(frameTimings[0].gpuFrameTime);
                    }

                    foreach (var counter in counters)
                        counter.CaptureLatest();

                    if (updateIntervalsMs.Count >= sampleCount)
                        Finish();
                }
                catch (Exception ex)
                {
                    Cleanup();
                    tcs.TrySetException(ex);
                }
            }

            EditorApplication.update += OnUpdate;
            return tcs.Task;
        }
#endif

        private static object BuildDistribution(IReadOnlyList<double> values)
        {
            if (values == null || values.Count == 0)
                return null;

            var sorted = values.OrderBy(value => value).ToArray();
            return new
            {
                average = Math.Round(values.Average(), 3),
                min = Math.Round(sorted[0], 3),
                max = Math.Round(sorted[sorted.Length - 1], 3),
                p50 = Math.Round(Percentile(sorted, 0.50), 3),
                p95 = Math.Round(Percentile(sorted, 0.95), 3),
                p99 = Math.Round(Percentile(sorted, 0.99), 3)
            };
        }

        private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
        {
            if (sortedValues.Count == 0)
                return 0d;

            var index = (sortedValues.Count - 1) * percentile;
            var lower = (int)Math.Floor(index);
            var upper = (int)Math.Ceiling(index);
            if (lower == upper)
                return sortedValues[lower];

            var weight = index - lower;
            return sortedValues[lower] + (sortedValues[upper] - sortedValues[lower]) * weight;
        }
    }

    [GPTAction("Reports current Unity memory counters with lightweight built-in APIs so the agent can baseline managed, reserved, and native usage before deeper memory snapshots.", Name = "probe_memory_counters")]
    public class ProbeMemoryCountersAction : GPTAssistantAction
    {
        [GPTParameter("Include a one-shot garbage collection before reading memory values. Use carefully because it affects runtime state.", Name = "force_gc_collect")]
        public bool ForceGcCollect { get; set; }

        public override async Task<string> Execute()
        {
            if (ForceGcCollect)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            var gcUsed = ProfileCounterCatalog.StartCounter(ProfileCounterCatalog.GcUsedMemory);
            var systemUsed = ProfileCounterCatalog.StartCounter(ProfileCounterCatalog.SystemUsedMemory);
            var totalReserved = ProfileCounterCatalog.StartCounter(ProfileCounterCatalog.TotalReservedMemory);
            var gfxUsed = ProfileCounterCatalog.StartCounter(ProfileCounterCatalog.GfxUsedMemory);
            var textureMemory = ProfileCounterCatalog.StartCounter(ProfileCounterCatalog.TextureMemory);

            try
            {
                gcUsed.CaptureLatest();
                systemUsed.CaptureLatest();
                totalReserved.CaptureLatest();
                gfxUsed.CaptureLatest();
                textureMemory.CaptureLatest();

                var payload = new
                {
                    capturedAtUtc = DateTime.UtcNow,
                    forceGcCollect = ForceGcCollect,
                    memoryBytes = new
                    {
                        totalAllocated = Profiler.GetTotalAllocatedMemoryLong(),
                        totalReserved = Profiler.GetTotalReservedMemoryLong(),
                        totalUnusedReserved = Profiler.GetTotalUnusedReservedMemoryLong(),
                        monoUsed = Profiler.GetMonoUsedSizeLong(),
                        monoHeap = Profiler.GetMonoHeapSizeLong(),
                        gcManaged = GC.GetTotalMemory(false)
                    },
                    counters = new
                    {
                        gcUsedMemory = gcUsed.ToLatestBytes(),
                        systemUsedMemory = systemUsed.ToLatestBytes(),
                        totalReservedMemory = totalReserved.ToLatestBytes(),
                        gfxUsedMemory = gfxUsed.ToLatestBytes(),
                        textureMemory = textureMemory.ToLatestBytes()
                    }
                };

                return JsonConvert.SerializeObject(payload, Formatting.Indented);
            }
            finally
            {
                gcUsed.Dispose();
                systemUsed.Dispose();
                totalReserved.Dispose();
                gfxUsed.Dispose();
                textureMemory.Dispose();
            }
        }
    }

    [GPTAction("Inspects the currently loaded scenes for common optimization risks such as heavy hierarchies, too many canvases, expensive light settings, and renderer/material fragmentation.", Name = "inspect_scene_optimization_risks")]
    public class InspectSceneOptimizationRisksAction : GPTAssistantAction
    {
        [GPTParameter("Include inactive scene objects in the audit.", Name = "include_inactive")]
        public bool IncludeInactive { get; set; } = true;

        [GPTParameter("Threshold for reporting a renderer as material-fragmented.", Name = "material_threshold")]
        public int MaterialThreshold { get; set; } = 3;

        [GPTParameter("Threshold for reporting a hierarchy as unusually large.", Name = "child_threshold")]
        public int ChildThreshold { get; set; } = 25;

        [GPTParameter("Maximum number of individual risks to return in each category.", Name = "max_results")]
        public int MaxResults { get; set; } = 20;

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            var renderers = new List<object>();
            var hierarchies = new List<object>();
            var lights = new List<object>();
            var canvases = new List<object>();

            var totals = new SceneAuditTotals();
            foreach (var root in EnumerateRootGameObjects())
            {
                Traverse(root.transform, totals, renderers, hierarchies, lights, canvases);
            }

            var risks = new List<object>();
            if (totals.lightCount > 8)
            {
                risks.Add(new { severity = "high", issue = "Many lights are active in loaded scenes.", evidence = new { totals.lightCount } });
            }

            if (totals.canvasCount > 6)
            {
                risks.Add(new { severity = "medium", issue = "Many canvases can increase rebuild cost.", evidence = new { totals.canvasCount } });
            }

            if (totals.particleSystemCount > 25)
            {
                risks.Add(new { severity = "medium", issue = "High particle system count may create update and overdraw pressure.", evidence = new { totals.particleSystemCount } });
            }

            if (totals.skinnedMeshRendererCount > 12)
            {
                risks.Add(new { severity = "medium", issue = "High skinned mesh count may increase CPU skinning or upload costs.", evidence = new { totals.skinnedMeshRendererCount } });
            }

            var payload = new
            {
                capturedAtUtc = DateTime.UtcNow,
                sceneCount = SceneManager.sceneCount,
                includeInactive = IncludeInactive,
                thresholds = new
                {
                    materialThreshold = Mathf.Max(1, MaterialThreshold),
                    childThreshold = Mathf.Max(5, ChildThreshold)
                },
                totals,
                summaryRisks = risks,
                heavyHierarchies = hierarchies.Take(Mathf.Max(1, MaxResults)).ToArray(),
                fragmentedRenderers = renderers.Take(Mathf.Max(1, MaxResults)).ToArray(),
                expensiveLights = lights.Take(Mathf.Max(1, MaxResults)).ToArray(),
                canvases = canvases.Take(Mathf.Max(1, MaxResults)).ToArray()
            };

            return JsonConvert.SerializeObject(payload, Formatting.Indented);
#else
            return "This action can only be run in the Unity Editor.";
#endif
        }

#if UNITY_EDITOR
        private IEnumerable<GameObject> EnumerateRootGameObjects()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                foreach (var root in scene.GetRootGameObjects())
                    yield return root;
            }
        }

        private void Traverse(
            Transform transform,
            SceneAuditTotals totals,
            List<object> renderers,
            List<object> hierarchies,
            List<object> lights,
            List<object> canvases)
        {
            if (transform == null)
                return;

            var gameObject = transform.gameObject;
            if (!IncludeInactive && !gameObject.activeInHierarchy)
                return;

            totals.gameObjectCount++;
            if (!gameObject.activeInHierarchy)
                totals.inactiveGameObjectCount++;

            var childCount = CountDescendants(transform);
            if (childCount >= Mathf.Max(5, ChildThreshold))
            {
                hierarchies.Add(new
                {
                    objectPath = GetHierarchyPath(gameObject),
                    descendantCount = childCount,
                    activeInHierarchy = gameObject.activeInHierarchy
                });
            }

            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (component == null)
                    continue;

                totals.componentCount++;
                switch (component)
                {
                    case Renderer renderer:
                        RegisterRenderer(renderer, totals, renderers);
                        break;
                    case Light light:
                        RegisterLight(light, totals, lights);
                        break;
                    case Canvas canvas:
                        RegisterCanvas(canvas, totals, canvases);
                        break;
                    case ParticleSystem:
                        totals.particleSystemCount++;
                        break;
                    case Animator:
                        totals.animatorCount++;
                        break;
                    case Rigidbody:
                        totals.rigidbodyCount++;
                        break;
                    case Collider:
                        totals.colliderCount++;
                        break;
                    case AudioSource:
                        totals.audioSourceCount++;
                        break;
                }
            }

            for (var i = 0; i < transform.childCount; i++)
                Traverse(transform.GetChild(i), totals, renderers, hierarchies, lights, canvases);
        }

        private void RegisterRenderer(Renderer renderer, SceneAuditTotals totals, List<object> renderers)
        {
            totals.rendererCount++;
            if (renderer is SkinnedMeshRenderer)
                totals.skinnedMeshRendererCount++;

            if (GameObjectUtility.AreStaticEditorFlagsSet(renderer.gameObject, StaticEditorFlags.BatchingStatic))
                totals.batchingStaticRendererCount++;
            else
                totals.nonBatchingStaticRendererCount++;

            var materialCount = renderer.sharedMaterials?.Count(material => material != null) ?? 0;
            if (materialCount >= Mathf.Max(1, MaterialThreshold))
            {
                renderers.Add(new
                {
                    objectPath = GetHierarchyPath(renderer.gameObject),
                    rendererType = renderer.GetType().Name,
                    materialCount,
                    castShadows = renderer.shadowCastingMode.ToString(),
                    receiveShadows = renderer.receiveShadows,
                    boundsSize = renderer.bounds.size.ToString()
                });
            }
        }

        private void RegisterLight(Light light, SceneAuditTotals totals, List<object> lights)
        {
            totals.lightCount++;
            if (light.shadows != LightShadows.None)
                totals.shadowCastingLightCount++;

            if (light.lightmapBakeType == LightmapBakeType.Realtime || light.lightmapBakeType == LightmapBakeType.Mixed)
            {
                lights.Add(new
                {
                    objectPath = GetHierarchyPath(light.gameObject),
                    type = light.type.ToString(),
                    mode = light.lightmapBakeType.ToString(),
                    shadows = light.shadows.ToString(),
                    range = Math.Round(light.range, 2),
                    intensity = Math.Round(light.intensity, 2)
                });
            }
        }

        private void RegisterCanvas(Canvas canvas, SceneAuditTotals totals, List<object> canvases)
        {
            totals.canvasCount++;
            var graphics = canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>(IncludeInactive).Length;
            if (graphics >= 25 || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                canvases.Add(new
                {
                    objectPath = GetHierarchyPath(canvas.gameObject),
                    renderMode = canvas.renderMode.ToString(),
                    pixelPerfect = canvas.pixelPerfect,
                    sortingOrder = canvas.sortingOrder,
                    graphicCount = graphics
                });
            }
        }

        private static int CountDescendants(Transform transform)
        {
            var total = 0;
            for (var i = 0; i < transform.childCount; i++)
            {
                total++;
                total += CountDescendants(transform.GetChild(i));
            }

            return total;
        }

        private static string GetHierarchyPath(GameObject gameObject)
        {
            var parts = new Stack<string>();
            var current = gameObject.transform;
            while (current != null)
            {
                parts.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", parts);
        }

        [Serializable]
        private class SceneAuditTotals
        {
            public int gameObjectCount;
            public int inactiveGameObjectCount;
            public int componentCount;
            public int rendererCount;
            public int skinnedMeshRendererCount;
            public int batchingStaticRendererCount;
            public int nonBatchingStaticRendererCount;
            public int lightCount;
            public int shadowCastingLightCount;
            public int canvasCount;
            public int particleSystemCount;
            public int animatorCount;
            public int rigidbodyCount;
            public int colliderCount;
            public int audioSourceCount;
        }
#endif
    }

    [GPTAction("Audits texture import settings for common optimization risks like read/write copies, oversized max sizes, disabled compression, and missing mipmaps on large textures.", Name = "inspect_texture_import_risks")]
    public class InspectTextureImportRisksAction : GPTAssistantAction
    {
        [GPTParameter("Folder to inspect, such as 'Assets' or 'Assets/Textures'.", Name = "search_path")]
        public string SearchPath { get; set; } = "Assets";

        [GPTParameter("Maximum texture count to include in the detailed findings.", Name = "max_results")]
        public int MaxResults { get; set; } = 50;

        [GPTParameter("Report textures at or above this importer max size as potentially expensive.", Name = "max_texture_size_threshold")]
        public int MaxTextureSizeThreshold { get; set; } = 2048;

        public override async Task<string> Execute()
        {
#if UNITY_EDITOR
            var searchPath = string.IsNullOrWhiteSpace(SearchPath) ? "Assets" : SearchPath;
            var results = new List<object>();
            var summary = new TextureAuditSummary();

            foreach (var guid in AssetDatabase.FindAssets("t:Texture", new[] { searchPath }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(assetPath) is TextureImporter importer))
                    continue;

                summary.inspectedTextureCount++;
                var issues = new List<string>();
                if (importer.isReadable)
                {
                    summary.readWriteEnabledCount++;
                    issues.Add("Read/Write Enabled duplicates texture memory.");
                }

                if (importer.maxTextureSize >= Mathf.Max(256, MaxTextureSizeThreshold))
                {
                    summary.oversizedTextureCount++;
                    issues.Add($"Max texture size is {importer.maxTextureSize}.");
                }

                if (importer.textureCompression == TextureImporterCompression.Uncompressed)
                {
                    summary.uncompressedTextureCount++;
                    issues.Add("Texture compression is disabled.");
                }

                if (!importer.mipmapEnabled && importer.textureType == TextureImporterType.Default && importer.maxTextureSize >= 1024)
                {
                    summary.largeTextureWithoutMipmapsCount++;
                    issues.Add("Large default texture has mipmaps disabled.");
                }

                if (issues.Count == 0)
                    continue;

                results.Add(new
                {
                    assetPath,
                    importer.textureType,
                    importer.textureCompression,
                    importer.maxTextureSize,
                    importer.mipmapEnabled,
                    importer.isReadable,
                    importer.npotScale,
                    issues
                });
            }

            return JsonConvert.SerializeObject(new
            {
                capturedAtUtc = DateTime.UtcNow,
                searchPath,
                thresholds = new
                {
                    maxTextureSizeThreshold = Mathf.Max(256, MaxTextureSizeThreshold)
                },
                summary,
                findings = results.Take(Mathf.Max(1, MaxResults)).ToArray()
            }, Formatting.Indented);
#else
            return "This action can only be run in the Unity Editor.";
#endif
        }

        [Serializable]
        private class TextureAuditSummary
        {
            public int inspectedTextureCount;
            public int readWriteEnabledCount;
            public int oversizedTextureCount;
            public int uncompressedTextureCount;
            public int largeTextureWithoutMipmapsCount;
        }
    }

    internal static class ProfileCounterCatalog
    {
        private static readonly Dictionary<string, ProfilerCategory> CategoriesByName =
            new Dictionary<string, ProfilerCategory>(StringComparer.OrdinalIgnoreCase)
            {
                ["Render"] = ProfilerCategory.Render,
                ["Memory"] = ProfilerCategory.Memory,
                ["Internal"] = ProfilerCategory.Internal,
                ["Scripts"] = ProfilerCategory.Scripts,
                ["Physics"] = ProfilerCategory.Physics,
                ["Audio"] = ProfilerCategory.Audio,
                ["Animation"] = ProfilerCategory.Animation,
                ["Loading"] = ProfilerCategory.Loading,
                ["GUI"] = ProfilerCategory.Gui,
                ["Video"] = ProfilerCategory.Video
            };

        internal static readonly CounterSpec MainThreadTime = new CounterSpec("main_thread_time", ProfilerCategory.Internal, "ns", "Main Thread", "Main Thread Time");
        internal static readonly CounterSpec RenderThreadTime = new CounterSpec("render_thread_time", ProfilerCategory.Render, "ns", "Render Thread", "Render Thread Time");
        internal static readonly CounterSpec DrawCalls = new CounterSpec("draw_calls", ProfilerCategory.Render, "count", "Draw Calls Count", "Draw Calls");
        internal static readonly CounterSpec Batches = new CounterSpec("batches", ProfilerCategory.Render, "count", "Batches Count", "Batches");
        internal static readonly CounterSpec SetPassCalls = new CounterSpec("setpass_calls", ProfilerCategory.Render, "count", "SetPass Calls Count", "SetPass Calls");
        internal static readonly CounterSpec Triangles = new CounterSpec("triangles", ProfilerCategory.Render, "count", "Triangles Count", "Triangles");
        internal static readonly CounterSpec Vertices = new CounterSpec("vertices", ProfilerCategory.Render, "count", "Vertices Count", "Vertices");
        internal static readonly CounterSpec GcUsedMemory = new CounterSpec("gc_used_memory", ProfilerCategory.Memory, "bytes", "GC Used Memory");
        internal static readonly CounterSpec SystemUsedMemory = new CounterSpec("system_used_memory", ProfilerCategory.Memory, "bytes", "System Used Memory");
        internal static readonly CounterSpec TotalReservedMemory = new CounterSpec("total_reserved_memory", ProfilerCategory.Memory, "bytes", "Total Reserved Memory");
        internal static readonly CounterSpec GfxUsedMemory = new CounterSpec("gfx_used_memory", ProfilerCategory.Memory, "bytes", "Gfx Used Memory", "Gfx Reserved Memory");
        internal static readonly CounterSpec TextureMemory = new CounterSpec("texture_memory", ProfilerCategory.Memory, "bytes", "Texture Memory");

        internal static IReadOnlyList<CounterSpec> CreateCuratedChecks()
        {
            return new[]
            {
                MainThreadTime,
                RenderThreadTime,
                DrawCalls,
                Batches,
                SetPassCalls,
                Triangles,
                Vertices,
                GcUsedMemory,
                SystemUsedMemory,
                TotalReservedMemory,
                GfxUsedMemory,
                TextureMemory
            };
        }

        internal static bool TryGetCategory(string raw, out ProfilerCategory category)
        {
            return CategoriesByName.TryGetValue(raw ?? string.Empty, out category);
        }

        internal static CounterSupportResult ProbeCounter(CounterSpec spec)
        {
            using var sample = StartCounter(spec);
            sample.CaptureLatest();
            return new CounterSupportResult
            {
                id = spec.Id,
                category = spec.Category.Name,
                candidateNames = spec.Names,
                available = sample.Valid,
                resolvedName = sample.ResolvedName,
                unit = spec.Unit
            };
        }

        internal static CounterSample StartCounter(CounterSpec spec)
        {
            foreach (var counterName in spec.Names)
            {
                try
                {
                    var recorder = ProfilerRecorder.StartNew(spec.Category, counterName, 8);
                    if (recorder.Valid)
                        return new CounterSample(spec, recorder, counterName);

                    recorder.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.Log($"[OptimizationProbe] Counter '{counterName}' in category '{spec.Category.Name}' is unavailable: {ex.Message}");
                }
            }

            return new CounterSample(spec, default, null);
        }

        internal readonly struct CounterSpec
        {
            internal CounterSpec(string id, ProfilerCategory category, string unit, params string[] names)
            {
                Id = id;
                Category = category;
                Unit = unit;
                Names = names ?? Array.Empty<string>();
            }

            internal string Id { get; }
            internal ProfilerCategory Category { get; }
            internal string Unit { get; }
            internal string[] Names { get; }
        }

        internal sealed class CounterSupportResult
        {
            public string id;
            public string category;
            public string[] candidateNames;
            public bool available;
            public string resolvedName;
            public string unit;
        }

        internal sealed class CounterSample : IDisposable
        {
            private readonly CounterSpec _spec;
            private ProfilerRecorder _recorder;
            private readonly List<long> _samples;

            internal CounterSample(CounterSpec spec, ProfilerRecorder recorder, string resolvedName)
            {
                _spec = spec;
                _recorder = recorder;
                _samples = new List<long>(64);
                ResolvedName = resolvedName;
            }

            internal bool Valid => _recorder.Valid;
            internal string ResolvedName { get; }

            internal void CaptureLatest()
            {
                if (!_recorder.Valid)
                    return;

                try
                {
                    _samples.Add(_recorder.LastValue);
                }
                catch
                {
                    // Ignore transient recorder read failures and keep the probe alive.
                }
            }

            internal object ToSummaryNsAsMs()
            {
                if (_samples.Count == 0)
                    return BuildEmpty();

                return new
                {
                    counter = ResolvedName,
                    unit = "ms",
                    sampleCount = _samples.Count,
                    average = Math.Round(_samples.Average() / 1_000_000d, 4),
                    latest = Math.Round(_samples[_samples.Count - 1] / 1_000_000d, 4),
                    min = Math.Round(_samples.Min() / 1_000_000d, 4),
                    max = Math.Round(_samples.Max() / 1_000_000d, 4)
                };
            }

            internal object ToSummaryRaw()
            {
                if (_samples.Count == 0)
                    return BuildEmpty();

                return new
                {
                    counter = ResolvedName,
                    unit = _spec.Unit,
                    sampleCount = _samples.Count,
                    average = Math.Round(_samples.Average(), 3),
                    latest = _samples[_samples.Count - 1],
                    min = _samples.Min(),
                    max = _samples.Max()
                };
            }

            internal object ToLatestBytes()
            {
                if (_samples.Count == 0)
                    return BuildEmpty();

                return new
                {
                    counter = ResolvedName,
                    unit = "bytes",
                    latest = _samples[_samples.Count - 1]
                };
            }

            private object BuildEmpty()
            {
                return new
                {
                    counter = ResolvedName,
                    unit = _spec.Unit,
                    sampleCount = 0,
                    available = false
                };
            }

            public void Dispose()
            {
                if (_recorder.Valid)
                    _recorder.Dispose();
            }
        }
    }
}
