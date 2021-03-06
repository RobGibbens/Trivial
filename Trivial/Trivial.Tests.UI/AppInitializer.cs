﻿using System;
using System.IO;
using System.Linq;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace Trivial.Tests.UI
{
    public class AppInitializer
    {
        public static IApp StartApp(Platform platform)
        {
            if (platform == Platform.Android)
            {
                return ConfigureApp
                    .Android
					.PreferIdeSettings()
                    .StartApp();
            }

            return ConfigureApp
                .iOS
				.AppBundle("../../../../testRuns/apps/TrivialiOS.app")
				//.PreferIdeSettings()
                .StartApp();
        }
    }
}

