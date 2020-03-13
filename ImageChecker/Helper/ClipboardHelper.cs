using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;

namespace ImageChecker.Helper
{
    public static class ClipboardHelper
    {
        public enum Operation
	    {
	        Copy,
            Cut
	    }

        public static void SetFilesToClipboard(IEnumerable<string> files, Operation operation)
        {
            StringCollection filePaths = new StringCollection();
            filePaths.AddRange(files.ToArray());

            byte[] moveEffect = new byte[] { 2, 0, 0, 0 };
            MemoryStream dropEffect = new MemoryStream();
            dropEffect.Write(moveEffect, 0, moveEffect.Length);

            DataObject data = new DataObject();
            data.SetFileDropList(filePaths);

            if (operation == Operation.Cut)
            {
                data.SetData("Preferred DropEffect", dropEffect); 
            }

            Clipboard.Clear();
            Clipboard.SetDataObject(data, true);
        }

        public static void SetFileToClipboard(string file, Operation operation)
        {
            StringCollection filePaths = new StringCollection();
            filePaths.Add(file);

            byte[] moveEffect = new byte[] { 2, 0, 0, 0 };
            MemoryStream dropEffect = new MemoryStream();
            dropEffect.Write(moveEffect, 0, moveEffect.Length);

            DataObject data = new DataObject();
            data.SetFileDropList(filePaths);

            if (operation == Operation.Cut)
            {
                data.SetData("Preferred DropEffect", dropEffect);
            }

            Clipboard.Clear();
            Clipboard.SetDataObject(data, true);
        }
    }
}
