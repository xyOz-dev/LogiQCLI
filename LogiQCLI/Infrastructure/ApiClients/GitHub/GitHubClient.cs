using LogiQCLI.Infrastructure.ApiClients.GitHub.Objects;
using Octokit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogiQCLI.Infrastructure.ApiClients.GitHub
{
    public class GitHubClientWrapper
    {
        private readonly GitHubClient _client;
        private readonly string? _token;

        public GitHubClientWrapper(string? token)
        {
            _token = token;
            
            if (string.IsNullOrWhiteSpace(token))
            {
                _client = new GitHubClient(new ProductHeaderValue("LogiQCLI"));
            }
            else
            {
                _client = new GitHubClient(new ProductHeaderValue("LogiQCLI"))
                {
                    Credentials = new Credentials(token)
                };
            }
        }

        public async Task<Repository> GetRepositoryAsync(string owner, string repo)
        {
            try
            {
                return await _client.Repository.Get(owner, repo);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get repository {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<Issue> CreateIssueAsync(string owner, string repo, NewIssue newIssue)
        {
            try
            {
                ValidateAuthentication();
                return await _client.Issue.Create(owner, repo, newIssue);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to create issue in {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<PullRequest> CreatePullRequestAsync(string owner, string repo, NewPullRequest newPullRequest)
        {
            try
            {
                ValidateAuthentication();
                return await _client.PullRequest.Create(owner, repo, newPullRequest);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to create pull request in {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<Issue>> GetIssuesAsync(string owner, string repo, RepositoryIssueRequest? request = null)
        {
            try
            {
                return await _client.Issue.GetAllForRepository(owner, repo, request ?? new RepositoryIssueRequest());
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get issues from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync(string owner, string repo, PullRequestRequest? request = null)
        {
            try
            {
                return await _client.PullRequest.GetAllForRepository(owner, repo, request ?? new PullRequestRequest());
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get pull requests from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<Reference> CreateBranchAsync(string owner, string repo, string branchName, string fromSha)
        {
            try
            {
                ValidateAuthentication();
                var newReference = new NewReference($"refs/heads/{branchName}", fromSha);
                return await _client.Git.Reference.Create(owner, repo, newReference);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to create branch {branchName} in {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<IssueComment> AddIssueCommentAsync(string owner, string repo, int issueNumber, string comment)
        {
            try
            {
                ValidateAuthentication();
                return await _client.Issue.Comment.Create(owner, repo, issueNumber, comment);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to add comment to issue #{issueNumber} in {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<Issue> UpdateIssueAsync(string owner, string repo, int issueNumber, IssueUpdate issueUpdate)
        {
            try
            {
                ValidateAuthentication();
                return await _client.Issue.Update(owner, repo, issueNumber, issueUpdate);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to update issue #{issueNumber} in {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<PullRequestMerge> MergePullRequestAsync(string owner, string repo, int pullRequestNumber, MergePullRequest mergePullRequest)
        {
            try
            {
                ValidateAuthentication();
                return await _client.PullRequest.Merge(owner, repo, pullRequestNumber, mergePullRequest);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to merge pull request #{pullRequestNumber} in {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<Reference> GetReferenceAsync(string owner, string repo, string reference)
        {
            try
            {
                return await _client.Git.Reference.Get(owner, repo, reference);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get reference {reference} from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<IssueComment> AddPullRequestCommentAsync(string owner, string repo, int pullRequestNumber, string comment)
        {
            try
            {
                ValidateAuthentication();
                return await _client.Issue.Comment.Create(owner, repo, pullRequestNumber, comment);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to add comment to pull request #{pullRequestNumber} in {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<Issue> GetIssueAsync(string owner, string repo, int issueNumber)
        {
            try
            {
                return await _client.Issue.Get(owner, repo, issueNumber);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get issue #{issueNumber} from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<PullRequest> GetPullRequestAsync(string owner, string repo, int pullRequestNumber)
        {
            try
            {
                return await _client.PullRequest.Get(owner, repo, pullRequestNumber);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get pull request #{pullRequestNumber} from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<IssueComment>> GetIssueCommentsAsync(string owner, string repo, int issueNumber)
        {
            try
            {
                return await _client.Issue.Comment.GetAllForIssue(owner, repo, issueNumber);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get comments for issue #{issueNumber} from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<PullRequestReviewComment>> GetPullRequestCommentsAsync(string owner, string repo, int pullRequestNumber)
        {
            try
            {
                return await _client.PullRequest.ReviewComment.GetAll(owner, repo, pullRequestNumber);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get comments for pull request #{pullRequestNumber} from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<PullRequest> UpdatePullRequestAsync(string owner, string repo, int pullRequestNumber, PullRequestUpdate pullRequestUpdate)
        {
            try
            {
                ValidateAuthentication();
                return await _client.PullRequest.Update(owner, repo, pullRequestNumber, pullRequestUpdate);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to update pull request #{pullRequestNumber} in {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<RepositoryContent>> GetRepositoryContentsAsync(string owner, string repo, string path = "")
        {
            try
            {
                return await _client.Repository.Content.GetAllContents(owner, repo, path);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get repository contents at path '{path}' from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<RepositoryContent>> GetFileContentAsync(string owner, string repo, string path)
        {
            try
            {
                return await _client.Repository.Content.GetAllContents(owner, repo, path);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get file content at path '{path}' from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<SearchCodeResult> SearchCodeAsync(string query, string owner = "", string repo = "")
        {
            try
            {
                var searchRequest = new SearchCodeRequest(query);
                if (!string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(repo))
                {
                    searchRequest.Repos.Add($"{owner}/{repo}");
                }
                return await _client.Search.SearchCode(searchRequest);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to search code with query '{query}': {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<Branch>> GetBranchesAsync(string owner, string repo)
        {
            try
            {
                return await _client.Repository.Branch.GetAll(owner, repo);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get branches from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<Branch> GetBranchAsync(string owner, string repo, string branchName)
        {
            try
            {
                return await _client.Repository.Branch.Get(owner, repo, branchName);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get branch '{branchName}' from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<GitHubCommit>> GetCommitsAsync(string owner, string repo, CommitRequest? request = null)
        {
            try
            {
                return await _client.Repository.Commit.GetAll(owner, repo, request ?? new CommitRequest());
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get commits from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<GitHubCommit> GetCommitAsync(string owner, string repo, string sha)
        {
            try
            {
                return await _client.Repository.Commit.Get(owner, repo, sha);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get commit '{sha}' from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<Label>> GetLabelsAsync(string owner, string repo)
        {
            try
            {
                return await _client.Issue.Labels.GetAllForRepository(owner, repo);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get labels from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<PullRequestFile>> GetPullRequestFilesAsync(string owner, string repo, int pullRequestNumber)
        {
            try
            {
                return await _client.PullRequest.Files(owner, repo, pullRequestNumber);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get files for pull request #{pullRequestNumber} from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<CompareResult> CompareBranchesAsync(string owner, string repo, string baseRef, string headRef)
        {
            try
            {
                return await _client.Repository.Commit.Compare(owner, repo, baseRef, headRef);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to compare {baseRef}...{headRef} in {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<PullRequestReview>> GetPullRequestReviewsAsync(string owner, string repo, int pullRequestNumber)
        {
            try
            {
                return await _client.PullRequest.Review.GetAll(owner, repo, pullRequestNumber);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get reviews for pull request #{pullRequestNumber} from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<string> GetPullRequestDiffAsync(string owner, string repo, int pullRequestNumber)
        {
            try
            {
                var pr = await _client.PullRequest.Get(owner, repo, pullRequestNumber);
                var diff = await _client.Connection.Get<string>(new Uri(pr.DiffUrl), null);
                return diff.Body;
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get diff for pull request #{pullRequestNumber} from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<string> GetCommitDiffAsync(string owner, string repo, string sha)
        {
            try
            {
                var commit = await _client.Repository.Commit.Get(owner, repo, sha);
                var diff = await _client.Connection.Get<string>(new Uri($"{commit.HtmlUrl}.diff"), null);
                return diff.Body;
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get diff for commit {sha} from {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<Notification>> GetNotificationsAsync(NotificationsRequest? request = null)
        {
            try
            {
                ValidateAuthentication();
                return await _client.Activity.Notifications.GetAllForCurrent(request ?? new NotificationsRequest());
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get notifications: {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<Notification>> GetRepositoryNotificationsAsync(string owner, string repo, NotificationsRequest? request = null)
        {
            try
            {
                ValidateAuthentication();
                return await _client.Activity.Notifications.GetAllForRepository(owner, repo, request ?? new NotificationsRequest());
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to get notifications for {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task MarkNotificationAsReadAsync(string notificationId)
        {
            try
            {
                ValidateAuthentication();
                await _client.Activity.Notifications.MarkAsRead(int.Parse(notificationId));
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to mark notification {notificationId} as read: {ex.Message}", ex);
            }
        }

        public async Task MarkAllNotificationsAsReadAsync()
        {
            try
            {
                ValidateAuthentication();
                await _client.Activity.Notifications.MarkAsRead();
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to mark all notifications as read: {ex.Message}", ex);
            }
        }

        public async Task<RepositoryContentChangeSet> CreateFileAsync(string owner, string repo, string path, CreateFileRequest request)
        {
            try
            {
                ValidateAuthentication();
                return await _client.Repository.Content.CreateFile(owner, repo, path, request);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to create file {path} in {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<RepositoryContentChangeSet> UpdateFileAsync(string owner, string repo, string path, UpdateFileRequest request)
        {
            try
            {
                ValidateAuthentication();
                return await _client.Repository.Content.UpdateFile(owner, repo, path, request);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to update file {path} in {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task DeleteFileAsync(string owner, string repo, string path, DeleteFileRequest request)
        {
            try
            {
                ValidateAuthentication();
                await _client.Repository.Content.DeleteFile(owner, repo, path, request);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to delete file {path} in {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<TreeResponse> CreateTreeAsync(string owner, string repo, NewTree tree)
        {
            try
            {
                ValidateAuthentication();
                return await _client.Git.Tree.Create(owner, repo, tree);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to create tree in {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<Commit> CreateCommitAsync(string owner, string repo, NewCommit commit)
        {
            try
            {
                ValidateAuthentication();
                return await _client.Git.Commit.Create(owner, repo, commit);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to create commit in {owner}/{repo}: {ex.Message}", ex);
            }
        }

        public async Task<Reference> UpdateReferenceAsync(string owner, string repo, string reference, ReferenceUpdate referenceUpdate)
        {
            try
            {
                ValidateAuthentication();
                return await _client.Git.Reference.Update(owner, repo, reference, referenceUpdate);
            }
            catch (Exception ex)
            {
                throw new GitHubClientException($"Failed to update reference {reference} in {owner}/{repo}: {ex.Message}", ex);
            }
        }

        private void ValidateAuthentication()
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                throw new GitHubClientException("This operation requires authentication. Please configure a GitHub token.");
            }
        }
    }

}