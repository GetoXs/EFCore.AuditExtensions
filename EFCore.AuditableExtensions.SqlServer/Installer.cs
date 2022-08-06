﻿using EFCore.AuditableExtensions.Common.Extensions;
using EFCore.AuditableExtensions.SqlServer.SqlGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.AuditableExtensions.SqlServer;

public static class Installer
{
    public static DbContextOptionsBuilder UseSqlServerAudit(this DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseAuditableExtension(AddSqlServerServices).ReplaceSqlServerServices();

    private static void AddSqlServerServices(this IServiceCollection services)
    {
        services.AddLogging();
        services.AddScoped<ICreateAuditTriggerSqlGenerator, CreateAuditTriggerSqlGenerator>();
        services.AddScoped<IDropAuditTriggerSqlGenerator, DropAuditTriggerSqlGenerator>();
    }

    private static DbContextOptionsBuilder ReplaceSqlServerServices(this DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IMigrationsSqlGenerator, MigrationsSqlGenerator>();

        return optionsBuilder;
    }
}