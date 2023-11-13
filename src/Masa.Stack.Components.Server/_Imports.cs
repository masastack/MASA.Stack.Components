﻿global using BlazorComponent;
global using BlazorComponent.I18n;
global using FluentValidation;
global using IdentityModel.Client;
global using Masa.Blazor;
global using Masa.Blazor.Presets;
global using Masa.BuildingBlocks.Authentication.Identity;
global using Masa.BuildingBlocks.Caching;
global using Masa.BuildingBlocks.StackSdks.Auth.Contracts;
global using Masa.BuildingBlocks.StackSdks.Auth.Contracts.Consts;
global using Masa.BuildingBlocks.StackSdks.Isolation;
global using Masa.Contrib.Caching.Distributed.StackExchangeRedis;
global using Masa.Contrib.StackSdks.Config;
global using Masa.Contrib.StackSdks.Isolation;
global using Masa.Contrib.StackSdks.Tsc;
global using Masa.Contrib.Storage.ObjectStorage.Aliyun;
global using Masa.Stack.Components.Infrastructure.Identity;
global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Authentication.Cookies;
global using Microsoft.AspNetCore.Authentication.OpenIdConnect;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.IdentityModel.Protocols.OpenIdConnect;
global using Microsoft.IdentityModel.Tokens;
global using System.Collections.Concurrent;
global using System.IdentityModel.Tokens.Jwt;
global using System.Security.Claims;
global using System.Text.Json.Nodes;
global using System.Text.RegularExpressions;
