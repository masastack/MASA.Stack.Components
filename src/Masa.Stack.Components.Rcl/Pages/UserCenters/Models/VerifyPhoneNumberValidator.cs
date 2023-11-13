// Copyright (c) MASA Stack All rights reserved.
// Licensed under the Apache License. See LICENSE.txt in the project root for license information.

namespace Masa.Stack.Components.Rcl.Pages.UserCenters.Models;

public class VerifyPhoneNumberValidator : AbstractValidator<VerifyMsgCodeModel>
{
    public VerifyPhoneNumberValidator()
    {
        RuleFor(user => user.Code)
            .NotEmpty()
            .WithMessage("captcha cannot be empty");
    }
}

