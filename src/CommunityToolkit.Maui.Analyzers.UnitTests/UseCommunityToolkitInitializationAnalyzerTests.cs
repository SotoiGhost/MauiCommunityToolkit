﻿using Xunit;
using static CommunityToolkit.Maui.Analyzers.UnitTests.CSharpCodeFixVerifier<CommunityToolkit.Maui.Analyzers.UseCommunityToolkitInitializationAnalyzer, CommunityToolkit.Maui.Analyzers.UseCommunityToolkitInitializationAnalyzerCodeFixProvider>;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;

public class UseCommunityToolkitInitializationAnalyzerTests
{
	[Fact]
	public void UseCommunityToolkitInitializationAnalyzerId()
	{
		Assert.Equal("MCT001", UseCommunityToolkitInitializationAnalyzer.DiagnosticId);
	}

	[Fact]
	public async Task VerifyNoErrorsWhenUseMauiCommunityToolkit()
	{
		const string source = @"
namespace CommunityToolkit.Maui.Analyzers.UnitTests
{
	using Microsoft.Maui.Controls.Hosting;
	using Microsoft.Maui.Hosting;
	using CommunityToolkit.Maui;

	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder.UseMauiApp<Microsoft.Maui.Controls.Application>()
				.UseMauiCommunityToolkit()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont(""OpenSans-Regular.ttf"", ""OpenSansRegular"");
					fonts.AddFont(""OpenSans-Semibold.ttf"", ""OpenSansSemibold"");
				});

			return builder.Build();
		}
	}
}";

		await VerifyAnalyzerAsync(source,
			[
				typeof(Options), // CommunityToolkit.Maui
				typeof(Core.Options), // CommunityToolkit.Maui.Core;
			]);
	}
}