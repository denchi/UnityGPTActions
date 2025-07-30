using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GPTUnity.Data
{
    [Serializable]
    public class GPTMessage
    {
        public string role;
        
        [JsonConverter(typeof(ContentFlexibleConverter))]
        public List<Content> content;

        // Tools Responses
        public GPTToolCall[] tool_calls;

        // Tools Completions
        public string tool_call_id;
        public string name;
        
        [Serializable]
        public class Content
        {
            public string type;
            public string text;
            public AudioContent input_audio;
        }

        [Serializable]
        public class AudioContent
        {
            public string data;
            public string format;
        }

        public string StringContent
        {
            get => content is { Count: > 0 } ? content.FirstOrDefault(x => x.type == "text")?.text : "";
            set
            {
                var firstTextContent = content?.FirstOrDefault(x => x.type == "text");
                if (firstTextContent == null)
                {
                    firstTextContent = new Content { type = "text" };
                    content = content?.Append(firstTextContent).ToList() ?? new List<Content>{ firstTextContent };
                }
                firstTextContent.text = value;
            }
        }

        public static IReadOnlyCollection<GPTMessage> CreateUserMessage(string content)
        {
            return new List<GPTMessage>
            {
                new GPTMessage
                {
                    role = "user",
                    StringContent = content
                }
            };
        }
        
        public static IReadOnlyCollection<GPTMessage> CreateUserMessage(string systemMessage, string content)
        {
            return new List<GPTMessage>
            {
                new GPTMessage
                {
                    role = "system",
                    StringContent = systemMessage
                },
                new GPTMessage
                {
                    role = "user",
                    StringContent = content
                }
            };
        }
        
        public static IReadOnlyCollection<GPTMessage> CreateInputAudioUserMessage(AudioClip audioClip)
        {
            byte[] wavBytes = AudioClipToWav(audioClip);
            string base64Audio = Convert.ToBase64String(wavBytes);
            
            byte[] AudioClipToWav(AudioClip clip)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    int samples = clip.samples * clip.channels;
                    float[] data = new float[samples];
                    clip.GetData(data, 0);

                    byte[] bytes = new byte[samples * 2];
                    int offset = 0;
                    foreach (float f in data)
                    {
                        short val = (short)(Mathf.Clamp(f, -1f, 1f) * short.MaxValue);
                        BitConverter.GetBytes(val).CopyTo(bytes, offset);
                        offset += 2;
                    }

                    int sampleRate = clip.frequency;
                    int channels = clip.channels;
                    int byteRate = sampleRate * channels * 2;

                    stream.Write(Encoding.ASCII.GetBytes("RIFF"));
                    stream.Write(BitConverter.GetBytes(36 + bytes.Length));
                    stream.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                    stream.Write(BitConverter.GetBytes(16)); // PCM chunk size
                    stream.Write(BitConverter.GetBytes((short)1)); // format
                    stream.Write(BitConverter.GetBytes((short)channels));
                    stream.Write(BitConverter.GetBytes(sampleRate));
                    stream.Write(BitConverter.GetBytes(byteRate));
                    stream.Write(BitConverter.GetBytes((short)(channels * 2))); // block align
                    stream.Write(BitConverter.GetBytes((short)16)); // bits per sample
                    stream.Write(Encoding.ASCII.GetBytes("data"));
                    stream.Write(BitConverter.GetBytes(bytes.Length));
                    stream.Write(bytes);

                    return stream.ToArray();
                }
            }
            
            return new List<GPTMessage>
            {
                new GPTMessage
                {
                    role = "system",
                    content = new List<Content>
                    {
                        new Content
                        {
                            type = "input_audio",
                            input_audio = new AudioContent
                            {
                                data = base64Audio,
                                format = "wav"
                            }
                        }
                    }
                }
            };
        }
        
        public class ContentFlexibleConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(List<GPTMessage.Content>);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var contentList = new List<GPTMessage.Content>();

                if (reader.TokenType == JsonToken.String)
                {
                    contentList.Add(new GPTMessage.Content
                    {
                        type = "text",
                        text = reader.Value.ToString()
                    });
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    var array = JArray.Load(reader);
                    foreach (var item in array)
                    {
                        var content = item.ToObject<GPTMessage.Content>(serializer);
                        contentList.Add(content);
                    }
                }

                return contentList;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var contentList = (List<GPTMessage.Content>)value;

                if (contentList.Count == 1 && contentList[0].type == "text" && contentList[0].input_audio == null)
                {
                    writer.WriteValue(contentList[0].text);
                }
                else
                {
                    serializer.Serialize(writer, contentList);
                }
            }
        }
    }
}