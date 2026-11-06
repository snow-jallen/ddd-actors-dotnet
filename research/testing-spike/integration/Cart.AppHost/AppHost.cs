var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddPostgres("pg").AddDatabase("carts");

builder.AddProject<Projects.Cart_Api>("cart-api")
       .WithReference(db)
       .WaitFor(db);

builder.Build().Run();
