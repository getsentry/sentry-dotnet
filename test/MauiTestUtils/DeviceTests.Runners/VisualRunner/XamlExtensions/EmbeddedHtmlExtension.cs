#nullable enable
using System;
using System.IO;
using Microsoft.Maui.Controls;

<<<<<<< HEAD
namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner
=======
namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner;

internal class EmbeddedHtmlExtension : EmbeddedResourceExtension
>>>>>>> chore/net8-devicetests
{
	class EmbeddedHtmlExtension : EmbeddedResourceExtension
	{
		public override object? ProvideValue(IServiceProvider serviceProvider)
		{
			if (base.ProvideValue(serviceProvider) is Stream stream)
			{
				using var reader = new StreamReader(stream, leaveOpen: false);
				return new HtmlWebViewSource { Html = reader.ReadToEnd() };
			}

<<<<<<< HEAD
			return null;
		}
	}
}
=======
        return null;
    }
}
>>>>>>> chore/net8-devicetests
