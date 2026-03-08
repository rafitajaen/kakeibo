## Prohibited Technologies

| Technology | Reason |
|------------|--------|
| EF Core InMemory, SQLite in-memory | Use Testcontainers |
| FluentAssertions | Use xUnit v3 native Assert.* methods |
