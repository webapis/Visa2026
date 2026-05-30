using System;
using Visa2026.Module.BusinessObjects.Feedback;

namespace Visa2026.Module.Services.Feedback;

public interface IUserFeedbackSubmitService
{
    Guid Submit(string userName, UserFeedbackSubmitInput input);
}
