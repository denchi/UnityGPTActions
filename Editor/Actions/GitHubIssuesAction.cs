using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Game.Environment;
using GPTUnity.Actions.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GPTUnity.Actions
{
    public enum GitHubIssueOperation
    {
        Get,
        Create,
        Update
    }
    
    public enum GitHubIssueState
    {
        Open,
        Closed,
        All
    }

    [GPTAction("Manage GitHub issues - get issues with filters, create new issues, or update existing ones.")]
    public class GitHubIssuesAction : GPTAssistantAction, IGPTActionThatContainsCode
    {
        [GPTParameter("Operation to perform: Get (fetch issues), Create (new issue), or Update (existing issue)", true)]
        public GitHubIssueOperation Operation { get; set; }
        
        [GPTParameter("Filter issues by state (for Get operation): Open, Closed, or All (default: Open)")]
        public GitHubIssueState State { get; set; }
        
        [GPTParameter("Filter issues by label (for Get operation, comma-separated labels)")]
        public string Labels { get; set; }
        
        [GPTParameter("Issue title (for Create/Update operations)")]
        public string Title { get; set; }
        
        [GPTParameter("Issue body/description (for Create/Update operations)")]
        public string Body { get; set; }
        
        [GPTParameter("Issue number (required for Update operation)")]
        public int IssueNumber { get; set; }
        
        [GPTParameter("Assignees for the issue (comma-separated usernames for Create/Update operations)")]
        public string Assignees { get; set; }
        
        [GPTParameter("Labels to apply to the issue (comma-separated for Create/Update operations)")]
        public string IssueLabels { get; set; }

        [GPTParameter("Set issue state when updating (for Update operation): Open or Closed")]
        public GitHubIssueState UpdateState { get; set; } = GitHubIssueState.Closed;
        
        private string _actualOwner;
        private string _actualRepo;
        
        public string Content
        {
            get
            {
                InitializeRepoInfo();
                return $"GitHub Issues: {Operation} - {_actualOwner}/{_actualRepo}";
            }
        }

        public override async Task<string> Execute()
        {
            if (!Env.TryGetEnv("GITHUB_TOKEN", out var githubToken))
            {
                throw new Exception("GitHub token is not set. Please set the GITHUB_TOKEN environment variable.");
            }
            
            InitializeRepoInfo();
            
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GPTAssistant", "1.0"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
                
                switch (Operation)
                {
                    case GitHubIssueOperation.Get:
                        return await GetIssues(client);
                    case GitHubIssueOperation.Create:
                        return await CreateIssue(client);
                    case GitHubIssueOperation.Update:
                        return await UpdateIssue(client);
                    default:
                        return "Invalid operation";
                }
            }
        }

        private void InitializeRepoInfo()
        {
            // Use environment variables if parameters not provided
            Env.TryGetEnv("GITHUB_OWNER", out _actualOwner);
            if (string.IsNullOrEmpty(_actualOwner))
            {
                throw new Exception("GitHub repository owner is required. Either provide Owner parameter or set GITHUB_OWNER in environment.");
            }
            
            if (string.IsNullOrEmpty(_actualRepo))
            {
                Env.TryGetEnv("GITHUB_REPO", out _actualRepo);
                if (string.IsNullOrEmpty(_actualRepo))
                {
                    throw new Exception("GitHub repository name is required. Either provide Repo parameter or set GITHUB_REPO in environment.");
                }
            }
        }

        private async Task<string> GetIssues(HttpClient client)
        {
            var baseUrl = $"https://api.github.com/repos/{_actualOwner}/{_actualRepo}/issues";
            
            // Get max results from environment or use default
            int perPage = 10;
            if (Env.TryGetEnv("GITHUB_MAX_RESULTS", out var maxResults) && 
                int.TryParse(maxResults, out var parsedMax))
            {
                perPage = parsedMax;
            }
            
            var queryParams = new StringBuilder($"?per_page={perPage}");
            
            queryParams.Append($"&state={State.ToString().ToLower()}");
            
            if (!string.IsNullOrEmpty(Labels))
            {
                queryParams.Append($"&labels={Uri.EscapeDataString(Labels)}");
            }
            
            var url = baseUrl + queryParams.ToString();
            
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return $"Failed to get issues: {response.StatusCode} - {response.ReasonPhrase}";

            var json = await response.Content.ReadAsStringAsync();
            var issues = JArray.Parse(json);
            
            if (issues.Count == 0)
                return "No issues found with the specified filters.";
                
            var result = new StringBuilder();
            result.AppendLine($"Found {issues.Count} issues in {_actualOwner}/{_actualRepo}:");
            
            foreach (var issue in issues)
            {
                result.AppendLine($"#{issue["number"]} - {issue["title"]}");
                result.AppendLine($"State: {issue["state"]}");
                result.AppendLine($"URL: {issue["html_url"]}");
                result.AppendLine();
            }
            
            return result.ToString();
        }
        
        private async Task<string> CreateIssue(HttpClient client)
        {
            if (string.IsNullOrEmpty(Title))
                return "Title is required for creating an issue.";
                
            var url = $"https://api.github.com/repos/{_actualOwner}/{_actualRepo}/issues";
            
            var issueData = new JObject();
            issueData["title"] = Title;
            
            if (!string.IsNullOrEmpty(Body))
                issueData["body"] = Body;
                
            if (!string.IsNullOrEmpty(Assignees))
            {
                var assigneeArray = new JArray();
                foreach (var assignee in Assignees.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    assigneeArray.Add(assignee.Trim());
                }
                issueData["assignees"] = assigneeArray;
            }
            // Use default assignees from environment if not provided
            else if (Env.TryGetEnv("GITHUB_DEFAULT_ASSIGNEES", out var defaultAssignees) && !string.IsNullOrEmpty(defaultAssignees))
            {
                var assigneeArray = new JArray();
                foreach (var assignee in defaultAssignees.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    assigneeArray.Add(assignee.Trim());
                }
                issueData["assignees"] = assigneeArray;
            }
            
            if (!string.IsNullOrEmpty(IssueLabels))
            {
                var labelsArray = new JArray();
                foreach (var label in IssueLabels.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    labelsArray.Add(label.Trim());
                }
                issueData["labels"] = labelsArray;
            }
            // Use default labels from environment if not provided
            else if (Env.TryGetEnv("GITHUB_DEFAULT_LABELS", out var defaultLabels) && !string.IsNullOrEmpty(defaultLabels))
            {
                var labelsArray = new JArray();
                foreach (var label in defaultLabels.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    labelsArray.Add(label.Trim());
                }
                issueData["labels"] = labelsArray;
            }
            
            var content = new StringContent(issueData.ToString(), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
                return $"Failed to create issue: {response.StatusCode} - {response.ReasonPhrase}";
                
            var json = await response.Content.ReadAsStringAsync();
            var issue = JObject.Parse(json);
            
            return $"Issue created successfully!\n" +
                   $"Number: #{issue["number"]}\n" +
                   $"Title: {issue["title"]}\n" +
                   $"URL: {issue["html_url"]}";
        }
        
        private async Task<string> UpdateIssue(HttpClient client)
        {
            if (IssueNumber <= 0)
                return "Issue number is required for updating an issue.";
                
            var url = $"https://api.github.com/repos/{_actualOwner}/{_actualRepo}/issues/{IssueNumber}";
            
            var issueData = new JObject();
            
            if (!string.IsNullOrEmpty(Title))
                issueData["title"] = Title;
                
            if (!string.IsNullOrEmpty(Body))
                issueData["body"] = Body;
                
            // Add state update if specified (and not "All" which is invalid for updates)
            issueData["state"] = UpdateState.ToString().ToLower();
                
            if (!string.IsNullOrEmpty(Assignees))
            {
                var assigneeArray = new JArray();
                foreach (var assignee in Assignees.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    assigneeArray.Add(assignee.Trim());
                }
                issueData["assignees"] = assigneeArray;
            }
            
            if (!string.IsNullOrEmpty(IssueLabels))
            {
                var labelsArray = new JArray();
                foreach (var label in IssueLabels.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    labelsArray.Add(label.Trim());
                }
                issueData["labels"] = labelsArray;
            }
            
            var content = new StringContent(issueData.ToString(), Encoding.UTF8, "application/json");
            var response = await client.PatchAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
                return $"Failed to update issue: {response.StatusCode} - {response.ReasonPhrase}";
                
            var json = await response.Content.ReadAsStringAsync();
            var issue = JObject.Parse(json);
            
            return $"Issue #{issue["number"]} updated successfully!\n" +
                   $"Title: {issue["title"]}\n" +
                   $"State: {issue["state"]}\n" + 
                   $"URL: {issue["html_url"]}";
        }
    }
}