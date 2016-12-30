using Microsoft.Tools.WindowsInstallerXml;

namespace SetupCustomize
{
    /*
     * Usage example in WiX:
     *
     * <?define SomeVersion="$(FileVersion.ProductVersion(Path-to-Some.dll))" ?>
     */
    public class FileVersion : WixExtension
    {
        private FileVersionPreprocessor _preprocessorExtension;

        public override PreprocessorExtension PreprocessorExtension => _preprocessorExtension ?? (_preprocessorExtension = new FileVersionPreprocessor());
    }
}
