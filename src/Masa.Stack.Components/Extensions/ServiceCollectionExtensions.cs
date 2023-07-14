using Masa.Contrib.StackSdks.Isolation;
using Masa.Contrib.StackSdks.Tsc;

namespace Masa.Stack.Components;

public static class ServiceCollectionExtensions
{
	//Consider only one web clearance for now
	public static async Task<IServiceCollection> AddMasaStackComponentsAsync(this IServiceCollection services, MasaStackProject project,
		string? i18nDirectoryPath = "wwwroot/i18n", string? authHost = null, string? mcHost = null,
		string? pmHost = null)
	{
		services.AddScoped<JsInitVariables>();
		services.AddAutoInject();
		services.AddMasaIdentity(options =>
		{
			options.UserName = "name";
			options.UserId = "sub";
			options.Role = IdentityClaimConsts.ROLES;
			options.Environment = IdentityClaimConsts.ENVIRONMENT;
			options.Mapping(nameof(MasaUser.CurrentTeamId), IdentityClaimConsts.CURRENT_TEAM);
			options.Mapping(nameof(MasaUser.StaffId), IdentityClaimConsts.STAFF);
			options.Mapping(nameof(MasaUser.Account), IdentityClaimConsts.ACCOUNT);
			options.Mapping(nameof(MasaUser.PhoneNumber), IdentityClaimConsts.PHONE_NUMBER);
			options.Mapping(nameof(MasaUser.Email), IdentityClaimConsts.EMAIL);
		});

		services.AddSingleton(sp => new ProjectAppOptions(project));

		services.AddMasaStackConfigAsync(project, MasaStackApp.WEB).ConfigureAwait(false).GetAwaiter().GetResult();
		var masaStackConfig = services.GetMasaStackConfig();

		authHost = authHost ?? masaStackConfig.GetAuthServiceDomain();
		mcHost = mcHost ?? masaStackConfig.GetMcServiceDomain();
		pmHost = pmHost ?? masaStackConfig.GetPmServiceDomain();
		var redisOption = new RedisConfigurationOptions
		{
			Servers = new List<RedisServerOptions> {
					new RedisServerOptions()
					{
						Host= masaStackConfig.RedisModel.RedisHost,
						Port= masaStackConfig.RedisModel.RedisPort
					}
				},
			DefaultDatabase = masaStackConfig.RedisModel.RedisDb,
			Password = masaStackConfig.RedisModel.RedisPassword
		};

		services.AddAuthClient(authHost, redisOption);
		services.AddSingleton(new McServiceOptions(mcHost));
		services.AddMcClient(mcHost);
		services.AddPmClient(pmHost);

		services.AddLogging(configure =>
		{
			// 创建配置构建器
			IConfiguration configuration = new ConfigurationBuilder()
				.AddEnvironmentVariables()
				.Build();
			var sericeName = masaStackConfig.GetWebId(project);
			services.AddObservable(configure, () => new MasaObservableOptions
			{
				ServiceNameSpace = masaStackConfig.Environment,
				ServiceVersion = masaStackConfig.Version,
				ServiceName = sericeName.IsNullOrEmpty() ? project.Name : sericeName,
				Layer = masaStackConfig.Namespace,
				ServiceInstanceId = configuration.GetValue<string>("HOSTNAME")
			}, () => masaStackConfig.OtlpUrl, true);
		});

		await services.AddStackIsolationAsync("");

		services.AddObjectStorage(option =>
		{
			option.UseAliyunStorage();
		});

		services.AddScoped((serviceProvider) =>
		{
			var masaUser = serviceProvider.GetRequiredService<IUserContext>().GetUser<MasaUser>() ?? new MasaUser();
			return masaUser;
		});

		var masaBuilder = services.AddMasaBlazor(options =>
		{
			options.ConfigureTheme(theme =>
			{
				theme.Themes.Light.Primary = "#4318FF";
				theme.Themes.Light.Accent = "#4318FF";
				theme.Themes.Light.Error = "#FF5252";
				theme.Themes.Light.Success = "#00B42A";
				theme.Themes.Light.Warning = "#FF7D00";
				theme.Themes.Light.Info = "#37A7FF";
			});
			options.Defaults = new Dictionary<string, IDictionary<string, object?>?>
			{
				{
					PopupComponents.SNACKBAR, new Dictionary<string, object?>
					{
						{ nameof(PEnqueuedSnackbars.Position), SnackPosition.BottomCenter }
					}
				}
			};
		})
		.AddI18n(GetLocales().ToArray());

		if (i18nDirectoryPath is not null)
		{
			masaBuilder.AddI18nForServer(i18nDirectoryPath);
		}

		return services;
	}

	private static IEnumerable<(string cultureName, Dictionary<string, string> map)> GetLocales()
	{
		var output = new List<(string cultureName, Dictionary<string, string> map)>();
		var assembly = typeof(ServiceCollectionExtensions).Assembly;
		var availableResources = assembly.GetManifestResourceNames()
										 .Select(s => Regex.Match(s, @"^.*Locales\.(.+)\.json"))
										 .Where(s => s.Success && s.Groups[1].Value != "supportedCultures")
										 .ToDictionary(s => s.Groups[1].Value, s => s.Value);
		foreach (var (cultureName, fileName) in availableResources)
		{
			using var fileStream = assembly.GetManifestResourceStream(fileName);
			if (fileStream is not null)
			{
				using var streamReader = new StreamReader(fileStream);
				var content = streamReader.ReadToEnd();
				var locale = I18nReader.Read(content);
				output.Add((cultureName, locale));
			}
		}
		return output;
	}
}
