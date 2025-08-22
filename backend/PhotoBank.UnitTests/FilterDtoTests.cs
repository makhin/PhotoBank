using NUnit.Framework;
using FluentAssertions;
using PhotoBank.ViewModel.Dto;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PhotoBank.Tests.ViewModel.Dto
{
    [TestFixture]
    public class FilterDtoTests
    {
        [Test]
        public void IsNotEmpty_ShouldReturnFalse_WhenAllPropertiesAreNullOrEmpty()
        {
            // Arrange
            var filterDto = new FilterDto();

            // Act
            var result = filterDto.IsNotEmpty();

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void IsNotEmpty_ShouldReturnTrue_WhenStoragesIsNotEmpty()
        {
            // Arrange
            var filterDto = new FilterDto
            {
                Storages = new List<int> { 1 }
            };

            // Act
            var result = filterDto.IsNotEmpty();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void IsNotEmpty_ShouldReturnTrue_WhenPersonsIsNotEmpty()
        {
            // Arrange
            var filterDto = new FilterDto
            {
                Persons = new List<int> { 1 }
            };

            // Act
            var result = filterDto.IsNotEmpty();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void IsNotEmpty_ShouldReturnTrue_WhenPersonNamesIsNotEmpty()
        {
            // Arrange
            var filterDto = new FilterDto
            {
                PersonNames = new[] { "John" }
            };

            // Act
            var result = filterDto.IsNotEmpty();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void IsNotEmpty_ShouldReturnTrue_WhenTagsIsNotEmpty()
        {
            // Arrange
            var filterDto = new FilterDto
            {
                Tags = new List<int> { 1 }
            };

            // Act
            var result = filterDto.IsNotEmpty();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void IsNotEmpty_ShouldReturnTrue_WhenTagNamesIsNotEmpty()
        {
            // Arrange
            var filterDto = new FilterDto
            {
                TagNames = new[] { "Nature" }
            };

            // Act
            var result = filterDto.IsNotEmpty();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void IsNotEmpty_ShouldReturnTrue_WhenPathsIsNotEmpty()
        {
            // Arrange
            var filterDto = new FilterDto
            {
                Paths = new List<int> { 1 }
            };

            // Act
            var result = filterDto.IsNotEmpty();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void IsNotEmpty_ShouldReturnTrue_WhenRelativePathIsNotEmpty()
        {
            // Arrange
            var filterDto = new FilterDto
            {
                RelativePath = "path"
            };

            // Act
            var result = filterDto.IsNotEmpty();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void IsNotEmpty_ShouldReturnTrue_WhenIsBWIsTrue()
        {
            // Arrange
            var filterDto = new FilterDto
            {
                IsBW = true
            };

            // Act
            var result = filterDto.IsNotEmpty();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void IsNotEmpty_ShouldReturnTrue_WhenIsAdultContentIsTrue()
        {
            // Arrange
            var filterDto = new FilterDto
            {
                IsAdultContent = true
            };

            // Act
            var result = filterDto.IsNotEmpty();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void IsNotEmpty_ShouldReturnTrue_WhenIsRacyContentIsTrue()
        {
            // Arrange
            var filterDto = new FilterDto
            {
                IsRacyContent = true
            };

            // Act
            var result = filterDto.IsNotEmpty();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void IsNotEmpty_ShouldReturnTrue_WhenThisDayIsNotNull()
        {
            // Arrange
            var filterDto = new FilterDto
            {
                ThisDay = new ThisDayDto { Day = 1, Month = 1 }
            };

            // Act
            var result = filterDto.IsNotEmpty();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void IsNotEmpty_ShouldReturnTrue_WhenTakenDateFromIsNotNull()
        {
            // Arrange
            var filterDto = new FilterDto
            {
                TakenDateFrom = DateTime.Now
            };

            // Act
            var result = filterDto.IsNotEmpty();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void IsNotEmpty_ShouldReturnTrue_WhenTakenDateToIsNotNull()
        {
            // Arrange
            var filterDto = new FilterDto
            {
                TakenDateTo = DateTime.Now
            };

            // Act
            var result = filterDto.IsNotEmpty();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void IsNotEmpty_ShouldReturnTrue_WhenCaptionIsNotEmpty()
        {
            // Arrange
            var filterDto = new FilterDto
            {
                Caption = "Sample Caption"
            };

            // Act
            var result = filterDto.IsNotEmpty();

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void Persons_ShouldBeIgnoredDuringSerialization()
        {
            var filterDto = new FilterDto
            {
                Persons = new List<int> { 1 }
            };

            var json = JsonSerializer.Serialize(filterDto, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            json.Should().NotContain("persons").And.NotContain("Persons");
        }

        [Test]
        public void Tags_ShouldBeIgnoredDuringSerialization()
        {
            var filterDto = new FilterDto
            {
                Tags = new List<int> { 1 }
            };

            var json = JsonSerializer.Serialize(filterDto, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            json.Should().NotContain("tags").And.NotContain("Tags");
        }
    }
}
