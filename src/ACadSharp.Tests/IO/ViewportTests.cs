using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Objects;
using ACadSharp.Tests.TestModels;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace ACadSharp.Tests.IO;

public class ViewportTests : IOTestsBase
{
	public ViewportTests(ITestOutputHelper output) : base(output)
	{
	}

	[Theory]
	[MemberData(nameof(DwgFilePaths))]
	[MemberData(nameof(DxfAsciiFiles))]
	public void ScaleInViewport(FileModel test)
	{
		CadDocument doc = this.readDocument(test);

		ACadSharp.Tables.BlockRecord paper = doc.PaperSpace;
		foreach (Viewport v in paper.Viewports)
		{
			Assert.NotNull(v.Scale);
		}
	}

	[Theory]
	[MemberData(nameof(DwgFilePaths))]
	[MemberData(nameof(DxfAsciiFiles))]
	public void VisualStyleInViewport(FileModel test)
	{
		CadDocument doc = this.readDocument(test);

		//The visual style reference of a viewport only exists from R2007 onwards.
		if (doc.Header.Version < ACadVersion.AC1021)
		{
			return;
		}

		Assert.True(doc.RootDictionary.TryGetEntry(CadDictionary.AcadVisualStyle, out CadDictionary styles));

		ACadSharp.Tables.BlockRecord paper = doc.PaperSpace;
		Assert.Contains(paper.Viewports, v => v.VisualStyle != null);

		//The reference has to be the very object held by the ACAD_VISUALSTYLE dictionary.
		foreach (Viewport v in paper.Viewports)
		{
			if (v.VisualStyle == null)
			{
				continue;
			}

			Assert.True(styles.TryGetEntry(v.VisualStyle.Name, out VisualStyle style));
			Assert.Same(style, v.VisualStyle);
		}
	}

	[Fact]
	public void VisualStyleIsResolvedFromHandle()
	{
		//A viewport references its visual style through the handle in the group code 348,
		//the visual style object itself lives in the OBJECTS section.
		string dxf = string.Join("\n",
			"0", "SECTION",
			"2", "ENTITIES",
			"0", "VIEWPORT",
			"5", "240",
			"100", "AcDbEntity",
			"8", "0",
			"100", "AcDbViewport",
			"10", "0.0",
			"20", "0.0",
			"30", "0.0",
			"40", "10.0",
			"41", "5.0",
			"68", "1",
			"69", "1",
			"281", "6",
			"348", "9F",
			"0", "ENDSEC",
			"0", "SECTION",
			"2", "OBJECTS",
			"0", "VISUALSTYLE",
			"5", "9F",
			"100", "AcDbVisualStyle",
			"2", "Conceptual",
			"70", "9",
			"0", "ENDSEC",
			"0", "EOF");

		CadDocument doc;
		using (MemoryStream stream = new MemoryStream(Encoding.ASCII.GetBytes(dxf)))
		using (DxfReader reader = new DxfReader(stream))
		{
			doc = reader.Read();
		}

		Viewport viewport = doc.Entities.OfType<Viewport>().Single();

		Assert.Equal(RenderMode.GouraudShadedWithWireframe, viewport.RenderMode);
		Assert.NotNull(viewport.VisualStyle);
		Assert.Equal(0x9FUL, viewport.VisualStyle.Handle);
	}
}
