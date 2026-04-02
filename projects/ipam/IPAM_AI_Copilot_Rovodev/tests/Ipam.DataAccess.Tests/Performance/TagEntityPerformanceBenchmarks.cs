using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Ipam.DataAccess.Entities;

namespace Ipam.DataAccess.Tests.Performance
{
    /// <summary>
    /// Performance benchmarks comparing TagEntity vs OptimizedTagEntity
    /// </summary>
    public class TagEntityPerformanceBenchmarks
    {
        private readonly ITestOutputHelper _output;

        public TagEntityPerformanceBenchmarks(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void PropertyAccess_RepeatedReads_OptimizedTagEntityShouldBeFaster()
        {
            // Arrange
            const int iterations = 10000;
            var testData = CreateTestData();

            var originalEntity = CreateOriginalTagEntity(testData);
            var optimizedEntity = CreateOptimizedTagEntity(testData);

            // Act & Measure Original TagEntity
            var originalTime = MeasurePropertyAccess(originalEntity, iterations);
            
            // Act & Measure OptimizedTagEntity  
            var optimizedTime = MeasurePropertyAccess(optimizedEntity, iterations);

            // Assert and Report
            var improvementRatio = originalTime.TotalMilliseconds / optimizedTime.TotalMilliseconds;
            
            _output.WriteLine($"=== Property Access Performance ({iterations:N0} iterations) ===");
            _output.WriteLine($"Original TagEntity:    {originalTime.TotalMilliseconds:F2}ms");
            _output.WriteLine($"Optimized TagEntity:   {optimizedTime.TotalMilliseconds:F2}ms");
            _output.WriteLine($"Performance improvement: {improvementRatio:F1}x faster");
            _output.WriteLine($"Time saved per access:   {(originalTime.TotalMilliseconds - optimizedTime.TotalMilliseconds) / iterations * 1000:F4}μs");

            // OptimizedTagEntity should be significantly faster
            Assert.True(improvementRatio > 5, $"Expected at least 5x improvement, got {improvementRatio:F1}x");
        }

        [Fact]
        public void PropertyModification_MultipleWrites_OptimizedTagEntityShouldBeFaster()
        {
            // Arrange
            const int modifications = 1000;
            var testData = CreateTestData();

            // Act & Measure Original TagEntity
            var originalTime = MeasurePropertyModifications(() => CreateOriginalTagEntity(testData), modifications);
            
            // Act & Measure OptimizedTagEntity
            var optimizedTime = MeasurePropertyModifications(() => CreateOptimizedTagEntity(testData), modifications);

            // Assert and Report
            var improvementRatio = originalTime.TotalMilliseconds / optimizedTime.TotalMilliseconds;
            
            _output.WriteLine($"=== Property Modification Performance ({modifications:N0} modifications) ===");
            _output.WriteLine($"Original TagEntity:    {originalTime.TotalMilliseconds:F2}ms");
            _output.WriteLine($"Optimized TagEntity:   {optimizedTime.TotalMilliseconds:F2}ms");
            _output.WriteLine($"Performance improvement: {improvementRatio:F1}x faster");
            _output.WriteLine($"Time saved per modification: {(originalTime.TotalMilliseconds - optimizedTime.TotalMilliseconds) / modifications:F4}ms");

            // OptimizedTagEntity should be faster due to batched serialization
            Assert.True(improvementRatio > 2, $"Expected at least 2x improvement, got {improvementRatio:F1}x");
        }

        [Fact]
        public void HelperMethods_OptimizedTagEntity_ShouldBeEfficient()
        {
            // Arrange
            var entity = CreateOptimizedTagEntity(CreateTestData());
            const int operations = 1000;

            // Act & Measure helper method performance
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < operations; i++)
            {
                entity.AddKnownValue($"Value{i}");
                entity.SetImplication($"Tag{i % 10}", $"CurrentValue{i}", $"ImpliedValue{i}");
                entity.SetAttribute($"Attr{i % 5}", $"Key{i}", $"Value{i}");
            }
            
            stopwatch.Stop();

            // Measure flush time
            var flushStopwatch = Stopwatch.StartNew();
            entity.FlushChanges();
            flushStopwatch.Stop();

            // Assert and Report
            _output.WriteLine($"=== Helper Methods Performance ({operations:N0} operations) ===");
            _output.WriteLine($"Total operation time:  {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
            _output.WriteLine($"Average per operation: {stopwatch.Elapsed.TotalMilliseconds / operations:F4}ms");
            _output.WriteLine($"Flush time:           {flushStopwatch.Elapsed.TotalMilliseconds:F2}ms");
            _output.WriteLine($"Has pending changes:  {entity.HasPendingChanges}");
            _output.WriteLine($"Final known values:   {entity.KnownValues.Count:N0}");
            _output.WriteLine($"Final implications:   {entity.Implies.Count:N0}");
            _output.WriteLine($"Final attributes:     {entity.Attributes.Count:N0}");

            // Helper methods should be very fast
            Assert.True(stopwatch.Elapsed.TotalMilliseconds / operations < 1, "Helper methods should be under 1ms per operation");
            Assert.False(entity.HasPendingChanges, "Entity should have no pending changes after flush");
        }

        [Fact]
        public void MemoryUsage_OptimizedTagEntity_ShouldBeReasonable()
        {
            // Arrange
            const int entityCount = 1000;
            var testData = CreateTestData();

            // Measure memory before
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memoryBefore = GC.GetTotalMemory(false);

            // Create entities
            var entities = new List<OptimizedTagEntity>();
            for (int i = 0; i < entityCount; i++)
            {
                entities.Add(CreateOptimizedTagEntity(testData));
            }

            // Access properties to trigger caching
            foreach (var entity in entities)
            {
                var _ = entity.KnownValues.Count + entity.Implies.Count + entity.Attributes.Count;
            }

            // Measure memory after
            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsed = memoryAfter - memoryBefore;
            var memoryPerEntity = (double)memoryUsed / entityCount;

            // Report
            _output.WriteLine($"=== Memory Usage ({entityCount:N0} entities) ===");
            _output.WriteLine($"Total memory used:     {memoryUsed / 1024.0:F2} KB");
            _output.WriteLine($"Memory per entity:     {memoryPerEntity:F2} bytes");
            _output.WriteLine($"Cache efficiency:      {(memoryPerEntity < 2048 ? "Good" : "High")}");

            // Memory usage should be reasonable (less than 2KB per entity with caching)
            Assert.True(memoryPerEntity < 2048, $"Memory usage too high: {memoryPerEntity:F2} bytes per entity");
        }

        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void ScalabilityTest_OptimizedTagEntity_ShouldScaleWell(int operationCount)
        {
            // Arrange
            var entity = CreateOptimizedTagEntity(CreateTestData());

            // Act - Measure scalability
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < operationCount; i++)
            {
                // Mix of read and write operations
                var _ = entity.KnownValues.Count;
                entity.AddKnownValue($"ScaleValue{i}");
                var __ = entity.Implies.Count;
                entity.SetImplication($"ScaleTag{i % 100}", $"Key{i}", $"Value{i}");
            }
            
            entity.FlushChanges();
            stopwatch.Stop();

            // Report
            var avgTimePerOp = stopwatch.Elapsed.TotalMilliseconds / operationCount;
            _output.WriteLine($"=== Scalability Test ({operationCount:N0} operations) ===");
            _output.WriteLine($"Total time:           {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
            _output.WriteLine($"Average per operation: {avgTimePerOp:F4}ms");
            _output.WriteLine($"Operations per second: {operationCount / stopwatch.Elapsed.TotalSeconds:F0}");

            // Performance should scale well (sub-millisecond per operation)
            Assert.True(avgTimePerOp < 1.0, $"Average operation time too high: {avgTimePerOp:F4}ms");
        }

        private static TestData CreateTestData()
        {
            return new TestData
            {
                KnownValues = new List<string> { "Dev", "Test", "Staging", "Prod", "Demo" },
                Implies = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Environment", new Dictionary<string, string> { { "Dev", "Development" }, { "Prod", "Production" } } },
                    { "Owner", new Dictionary<string, string> { { "TeamA", "team-a@company.com" }, { "TeamB", "team-b@company.com" } } }
                },
                Attributes = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Priority", new Dictionary<string, string> { { "High", "1" }, { "Medium", "2" }, { "Low", "3" } } },
                    { "Cost", new Dictionary<string, string> { { "Expensive", "100" }, { "Cheap", "10" } } }
                }
            };
        }

        private static TagEntity CreateOriginalTagEntity(TestData data)
        {
            return new TagEntity
            {
                PartitionKey = "test-space",
                RowKey = "test-tag",
                Name = "test-tag",
                Type = "Inheritable",
                Description = "Test tag for performance comparison",
                KnownValues = data.KnownValues,
                Implies = data.Implies,
                Attributes = data.Attributes,
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };
        }

        private static OptimizedTagEntity CreateOptimizedTagEntity(TestData data)
        {
            var entity = new OptimizedTagEntity
            {
                PartitionKey = "test-space",
                RowKey = "test-tag",
                Name = "test-tag",
                Type = "Inheritable",
                Description = "Test tag for performance comparison",
                KnownValues = data.KnownValues,
                Implies = data.Implies,
                Attributes = data.Attributes,
                CreatedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow
            };
            
            entity.FlushChanges(); // Ensure initial state is flushed
            return entity;
        }

        private static TimeSpan MeasurePropertyAccess<T>(T entity, int iterations) where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                // Use reflection to access properties dynamically to ensure fair comparison
                var knownValues = GetProperty<List<string>>(entity, "KnownValues");
                var implies = GetProperty<Dictionary<string, Dictionary<string, string>>>(entity, "Implies");
                var attributes = GetProperty<Dictionary<string, Dictionary<string, string>>>(entity, "Attributes");
                
                // Access some values to prevent compiler optimizations
                var totalCount = knownValues.Count + implies.Count + attributes.Count;
            }
            
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        private static TimeSpan MeasurePropertyModifications<T>(Func<T> entityFactory, int modifications) where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < modifications; i++)
            {
                var entity = entityFactory();
                
                // Modify properties
                var knownValues = GetProperty<List<string>>(entity, "KnownValues");
                knownValues.Add($"NewValue{i}");
                SetProperty(entity, "KnownValues", knownValues);
                
                var implies = GetProperty<Dictionary<string, Dictionary<string, string>>>(entity, "Implies");
                implies[$"NewTag{i}"] = new Dictionary<string, string> { { $"Key{i}", $"Value{i}" } };
                SetProperty(entity, "Implies", implies);
                
                // For OptimizedTagEntity, flush changes
                if (entity is OptimizedTagEntity optimized)
                {
                    optimized.FlushChanges();
                }
            }
            
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        private static TProperty GetProperty<TProperty>(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName);
            return (TProperty)property!.GetValue(obj)!;
        }

        private static void SetProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            property!.SetValue(obj, value);
        }

        private class TestData
        {
            public List<string> KnownValues { get; set; } = new();
            public Dictionary<string, Dictionary<string, string>> Implies { get; set; } = new();
            public Dictionary<string, Dictionary<string, string>> Attributes { get; set; } = new();
        }
    }
}