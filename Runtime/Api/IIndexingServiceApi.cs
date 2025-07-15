using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GPTUnity.Indexing
{
    public interface IIndexingServiceApi
    {
        Task<List<SearchResult>> SearchAsync(string query, int maxResults = 10, int topK = 5);
        Task<bool> StartSearchServerAsync();
        Task<bool> StopSearchServer();
        Task<bool> IsServerAvailable();
    }
    
    [Serializable]
    public class SearchResult
    {
        public string file;
        public string type;
        public string name;
        public string content;
    }

    [Serializable]
    public class SearchResultList
    {
        public List<SearchResult> results;
    }

    [Serializable]
    public class SearchRequest
    {
        public string query;
        public int max_results = 10;
        public int top_k = 5;
    }
    
    //
} 
