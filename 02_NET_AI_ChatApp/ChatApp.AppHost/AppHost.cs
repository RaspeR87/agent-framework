using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

var aiChatApp = builder.AddProject<Projects.ChatApp_Web>("chatapp-web");

// Optional: add vector store or local database here later
// var db = builder.AddPostgres("postgres").WithDataVolume();
// aiChatApp.WithReference(db);

builder.Build().Run();