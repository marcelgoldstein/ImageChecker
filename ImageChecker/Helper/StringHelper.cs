using System;
using System.Text;

namespace ImageChecker.Helper
{
    public static class StringHelper
    {
        public static string GetFileSizeString(int size)
        {
            StringBuilder builder = new StringBuilder();
            string str = (Math.Ceiling(size / 1024.0)).ToString();
            int start = str.Length % 3;
            switch (start)
            {
                case 0:
                    break;
                case 1:
                    builder.Append(str[0]);
                    builder.Append(" ");
                    break;
                case 2:
                    builder.Append(str[0]);
                    builder.Append(str[1]);
                    builder.Append(" ");
                    break;
            }
            for (int i = start; i < str.Length; i += 3)
            {
                builder.Append(str[i + 0]);
                builder.Append(str[i + 1]);
                builder.Append(str[i + 2]);
                builder.Append(" ");
            }
            builder.Append("KB");
            return builder.ToString();
        }
    }
}
