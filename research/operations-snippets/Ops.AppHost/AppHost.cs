var builder = DistributedApplication.CreateBuilder(args);

var pgPassword = builder.AddParameter("pg-password", secret: true);
var postgres = builder.AddPostgres("postgres", password: pgPassword)
                      .WithDataVolume("orders-pgdata")      // survives AppHost restarts
                      .WithLifetime(ContainerLifetime.Persistent)
                      .WithPgAdmin();
var journalDb = postgres.AddDatabase("journal");

// One-shot schema migrator (applies Akka.Persistence.Sql DDL + read-model migrations), then exits.
var migrator = builder.AddProject<Projects.Ops_Verify>("migrator")
                      .WithReference(journalDb)
                      .WaitFor(journalDb);

builder.AddProject<Projects.Ops_Node>("orders")
       .WithReference(journalDb)                  // ConnectionStrings__journal
       .WaitForCompletion(migrator)               // never start nodes against an unmigrated schema
       .WithEnvironment("Akka__AppVersion", "1.5.0")
       .WithEnvironment("FeatureManagement__NewPricing", "false")
       .WithReplicas(1);

builder.Build().Run();
