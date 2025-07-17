using NUnit.Framework;
using FluentAssertions;
using PhotoBank.ViewModel.Dto;
using System;
using System.Collections.Generic;

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
        public void IsNotEmpty_ShouldReturnTrue_WhenThisDayIsTrue()
        {
            // Arrange
            var filterDto = new FilterDto
            {
                ThisDay = true
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
    }
}
