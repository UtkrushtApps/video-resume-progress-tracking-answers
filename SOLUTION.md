# Solution Steps

1. Add a unique index on PlaybackProgress for the composite key (UserId, VideoId) in StreamingDbContext.OnModelCreating. This makes the data model enforce one progress row per user/video pair.

2. Validate the target video before writing progress. Query Videos asynchronously with the supplied CancellationToken; return VideoNotFound if no video exists for the route videoId.

3. Validate PositionSeconds after loading the video duration. Return InvalidPosition for negative values or values greater than the video duration, before inserting or updating any PlaybackProgress row.

4. Replace the race-prone read-then-insert/update logic with one atomic database operation. Use an async EF Core ExecuteSqlInterpolatedAsync MERGE statement with HOLDLOCK keyed by UserId and VideoId.

5. In the MERGE statement, insert when no row exists; when a row does exist, only update it if the incoming PositionSeconds is greater than the stored PositionSeconds. This makes out-of-order heartbeats idempotent and prevents backward progress.

6. After the atomic write, load the persisted row asynchronously with AsNoTracking and project it to ProgressResponse. Return that response with ProgressOutcome.Saved so successful responses consistently show the saved position.

7. Pass the request CancellationToken to every EF Core async call: video lookup, MERGE execution, and response lookup.

8. Update the controller to translate service outcomes into HTTP responses: 200 for Saved, 400 for InvalidPosition, and 404 for VideoNotFound, keeping the route POST /api/videos/{videoId}/progress unchanged.

9. Add controller handling for a null body and a generic catch-all 500 response that does not expose exception details. Re-throw cancellation exceptions when the request CancellationToken has been canceled.

10. Optionally add an async DbInitializer.InitializeAsync and use it from Program.cs so application startup database initialization is also asynchronous, while retaining the existing synchronous Initialize method for tests.

