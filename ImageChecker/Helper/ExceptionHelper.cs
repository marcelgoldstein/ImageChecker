using System;

namespace ImageChecker.Helper;

static class ExceptionHelper
{
    public static Exception GetInnerMostException(this Exception ex)
    {
        if (ex.InnerException == null)
        {
            return ex;
        }
        else
        {
            return ex.GetInnerMostException();
        }
    }
}
