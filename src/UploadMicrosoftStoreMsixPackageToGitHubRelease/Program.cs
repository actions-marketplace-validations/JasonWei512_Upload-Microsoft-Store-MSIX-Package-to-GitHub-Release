﻿using CommandLine;
using Octokit;
using UploadMicrosoftStoreMsixPackageToGitHubRelease;

async Task Start(Options options)
{
    if (Environment.GetEnvironmentVariable("GITHUB_REPOSITORY") is not string gitHubRepoEnv)  // Env "GITHUB_REPOSITORY" is like "owner/repo"
    {
        throw new Exception(@"Cannot get environment variable ""GITHUB_REPOSITORY""");
    }

    string[] gitHubRepoEnvSplits = gitHubRepoEnv.Split("/");
    string gitHubRepoOwner = gitHubRepoEnvSplits[0];
    string gitHubRepoName = gitHubRepoEnvSplits[1];

    if (options.DryRun)
    {
        Console.WriteLine("This is a dry run, so the files won't be uploaded.");
        Console.WriteLine();
    }

    GitHubHelper.SetToken(options.Token);

    IReadOnlyList<MsixPackage> msixPackages = await StoreHelper.GetLatestMsixPacakgeInfo(options.StoreID);

    if (!msixPackages.Any())
    {
        throw new Exception($@"App with ID ""{options.StoreID}"" not found.");
    }

    Release gitHubRelease = await GitHubHelper.GetLatestGitHubRelease(msixPackages, gitHubRepoOwner, gitHubRepoName);

    Console.WriteLine($"Found GitHub release: {gitHubRelease.TagName}");

    await GitHubHelper.UploadMsixPackagesToGitHubRelease(gitHubRelease, msixPackages, options.AssetNamePattern, options.DryRun);

    Console.WriteLine("Exiting ...");
}

await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(async options =>
{
    try
    {
        await Start(options);
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error: {e.Message}");
        Console.WriteLine();
        Console.WriteLine(e);

        Environment.Exit(1);
    }
});

public class Options
{
    [Option("store-id", Required = true, HelpText = @"The ID of the Microsoft Store app you want to download, e.g. ""9N0DX20HK701"".")]
    public string StoreID { get; set; } = string.Empty;

    [Option("token", Required = true, HelpText = "The GitHub token to use.")]
    public string Token { get; set; } = string.Empty;

    [Option("dry-run", HelpText = "Do not perform the actual upload. For testing.")]
    public bool DryRun { get; set; } = false;

    [Option("asset-name-pattern", HelpText = """
        The name pattern of the uploaded GitHub release asset's name without file extension. Can contain "{version}" and "{arch}". 
        For example, for pattern "App_{version}_{arch}", the uploaded asset name can be "App_1.2.3.0_x64.Msix".
        """)]
    public string? AssetNamePattern { get; set; }
}