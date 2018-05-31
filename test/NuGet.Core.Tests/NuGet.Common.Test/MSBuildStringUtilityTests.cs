using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NuGet.Common.Test
{
    public class MSBuildStringUtilityTests
    {
        [Fact]
        public void GetSingleOrDefaultDistinctNuGetLogCodes_SameLogCodes()
        {
            // Arrange
            var logCodes1 = new List<NuGetLogCode>() { NuGetLogCode.NU1000, NuGetLogCode.NU1001};
            var logCodes2 = new List<NuGetLogCode>() { NuGetLogCode.NU1001, NuGetLogCode.NU1000,};

            var logCodesList = new List<IEnumerable<NuGetLogCode>>() { logCodes1, logCodes2 };

            // Act
            var result = MSBuildStringUtility.GetSingleOrDefaultDistinctNuGetLogCodes(logCodesList);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.True(result.All(logCodes2.Contains));
        }

        [Fact]
        public void GetSingleOrDefaultDistinctNuGetLogCodes_EmptyLogCodes()
        {
            // Arrange
            var logCodesList = new List<IEnumerable<NuGetLogCode>>();

            // Act
            var result = MSBuildStringUtility.GetSingleOrDefaultDistinctNuGetLogCodes(logCodesList);

            // Assert
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public void GetSingleOrDefaultDistinctNuGetLogCodes_DiffLogCodes()
        {
            // Arrange
            var logCodes1 = new List<NuGetLogCode>() { NuGetLogCode.NU1000};
            var logCodes2 = new List<NuGetLogCode>() { NuGetLogCode.NU1001, NuGetLogCode.NU1000 };

            var logCodesList = new List<IEnumerable<NuGetLogCode>>() { logCodes1, logCodes2 };

            // Act
            var result = MSBuildStringUtility.GetSingleOrDefaultDistinctNuGetLogCodes(logCodesList);

            // Assert
            Assert.Equal(0, result.Count());
        }
    }
}
