using FluentValidation.Results;
using Helsi.TodoApi.Api.Contracts;
using Helsi.TodoApi.Api.Validation;
using Helsi.TodoApi.Application.Common;

namespace Helsi.TodoApi.UnitTests.Validation
{
    public sealed class RequestValidatorTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_EmptyTitle_FailsWithTitleEmpty(string title)
        {
            ValidationResult result = new CreateTodoListRequestValidator().Validate(new CreateTodoListRequest(title));

            Assert.Equal(TodoListErrors.TitleEmpty.Code, Assert.Single(result.Errors).ErrorCode);
        }

        [Fact]
        public void Create_TitleLongerThan255_FailsWithTitleTooLong()
        {
            ValidationResult result = new CreateTodoListRequestValidator().Validate(new CreateTodoListRequest(new string('a', 256)));

            Assert.Equal(TodoListErrors.TitleTooLong.Code, Assert.Single(result.Errors).ErrorCode);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(255)]
        public void Create_TitleWithinLimits_Passes(int length)
        {
            ValidationResult result = new CreateTodoListRequestValidator().Validate(new CreateTodoListRequest(new string('a', length)));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Create_TitleOf255WithSurroundingSpaces_Passes()
        {
            ValidationResult result = new CreateTodoListRequestValidator().Validate(new CreateTodoListRequest("  " + new string('a', 255) + "  "));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Update_UsesTheSameTitleRules()
        {
            ValidationResult result = new UpdateTodoListRequestValidator().Validate(new UpdateTodoListRequest(" "));

            Assert.Equal(TodoListErrors.TitleEmpty.Code, Assert.Single(result.Errors).ErrorCode);
        }

        [Fact]
        public void Share_EmptyUserId_FailsWithUserRequired()
        {
            ValidationResult result = new ShareTodoListRequestValidator().Validate(new ShareTodoListRequest(Guid.Empty));

            Assert.Equal(TodoListErrors.UserRequired.Code, Assert.Single(result.Errors).ErrorCode);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Feed_PageBelowOne_FailsWithPageInvalid(int page)
        {
            ValidationResult result = new GetTodoListsQueryValidator().Validate(new GetTodoListsQuery { Page = page });

            Assert.Equal(TodoListErrors.PageInvalid.Code, Assert.Single(result.Errors).ErrorCode);
        }

        [Fact]
        public void Feed_PageThatWouldOverflowTheSkip_FailsWithPageInvalid()
        {
            ValidationResult result = new GetTodoListsQueryValidator().Validate(new GetTodoListsQuery { Page = int.MaxValue, PageSize = 100 });

            Assert.Equal(TodoListErrors.PageInvalid.Code, Assert.Single(result.Errors).ErrorCode);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public void Feed_PageSizeOutOfRange_FailsWithPageSizeInvalid(int pageSize)
        {
            ValidationResult result = new GetTodoListsQueryValidator().Validate(new GetTodoListsQuery { PageSize = pageSize });

            Assert.Equal(TodoListErrors.PageSizeInvalid.Code, Assert.Single(result.Errors).ErrorCode);
        }

        [Fact]
        public void Feed_Defaults_Pass()
        {
            Assert.True(new GetTodoListsQueryValidator().Validate(new GetTodoListsQuery()).IsValid);
        }
    }
}
