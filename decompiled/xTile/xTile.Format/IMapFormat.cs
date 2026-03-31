using System.IO;

namespace xTile.Format;

public interface IMapFormat
{
	string Name { get; }

	string FileExtensionDescriptor { get; }

	string FileExtension { get; }

	CompatibilityReport DetermineCompatibility(Map map);

	Map Load(Stream stream);

	void Store(Map map, Stream stream);
}
