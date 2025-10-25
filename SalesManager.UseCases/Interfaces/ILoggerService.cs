using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SalesManager.UseCases.Interfaces
{
    public interface ILoggerService
    {
        void LogInfo(string message);
        void LogWarn(string message, Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex);
        void LogDebug(string message);
        void LogError(string message, Exception? ex = null);
    }
}