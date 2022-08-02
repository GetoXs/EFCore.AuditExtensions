﻿using System.Data;
using EFCore.AuditableExtensions.Common;
using EFCore.AuditableExtensions.Common.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartFormat;

namespace EFCore.AuditableExtensions.SqlServer.SqlGenerators;

internal interface ICreateAuditTriggerSqlGenerator
{
    void Generate(CreateAuditTriggerOperation operation, MigrationCommandListBuilder builder);
}

internal class CreateAuditTriggerSqlGenerator : ICreateAuditTriggerSqlGenerator
{
    private const string BaseSql = @"
    CREATE TRIGGER {TriggerName} ON {AuditedEntityTableName}
    FOR {OperationType} AS
    BEGIN
    DECLARE @user varchar(255)
    SELECT @user = CAST(SESSION_CONTEXT(N'user') AS varchar(255))
     
    INSERT INTO {AuditTableName} (
        {KeyColumnName},
        {OldDataColumnName},
        {NewDataColumnName},
        {OperationTypeColumnName},
        {UserColumnName}
        {TimestampColumnName},
    )
    VALUES(
        (SELECT {KeyColumnName} FROM {KeySource}),
        {OldRowDataSql},
        {NewRowDataSql},
        '{OperationType}',
        @user,
        GETUTCDATE()
    );
    END";

    public void Generate(CreateAuditTriggerOperation operation, MigrationCommandListBuilder builder)
    {
        foreach (var sqlLine in BaseSql.Split('\n'))
        {
            builder.AppendLine(ReplacePlaceholders(sqlLine, operation));
        }

        builder.EndCommand();
    }

    private static string ReplacePlaceholders(string sql, CreateAuditTriggerOperation operation)
    {
        var parameters = GetSqlParameters(operation);
        return Smart.Format(sql, parameters);
    }

    private static CreateAuditTriggerSqlParameters GetSqlParameters(CreateAuditTriggerOperation operation)
    {
        string GetKeySource(StatementType statementType) => statementType switch
        {
            StatementType.Insert or StatementType.Update => "Inserted",
            StatementType.Delete                         => "Deleted",
            _                                            => throw new ArgumentOutOfRangeException(nameof(statementType), statementType, "Value not supported"),
        };

        string GetOldRowDataSql(StatementType statementType) => statementType switch
        {
            StatementType.Insert                         => "null",
            StatementType.Update or StatementType.Delete => "(SELECT * FROM Deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)",
            _                                            => throw new ArgumentOutOfRangeException(nameof(statementType), statementType, "Value not supported"),
        };

        string GetNewRowDataSql(StatementType statementType) => statementType switch
        {
            StatementType.Insert or StatementType.Update => "(SELECT * FROM Inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)",
            StatementType.Delete                         => "null",
            _                                            => throw new ArgumentOutOfRangeException(nameof(statementType), statementType, "Value not supported"),
        };

        return new CreateAuditTriggerSqlParameters
        {
            TriggerName = operation.TriggerName,
            AuditedEntityTableName = operation.AuditedEntityTableName,
            AuditTableName = operation.AuditTableName,
            OperationType = operation.OperationType.ToString().ToUpper(),
            KeyColumnName = operation.AuditedEntityTableKeyColumnName,
            OldRowDataSql = GetOldRowDataSql(operation.OperationType),
            NewRowDataSql = GetNewRowDataSql(operation.OperationType),
            KeySource = GetKeySource(operation.OperationType),
            OldDataColumnName = Constants.AuditTableColumnNames.OldData,
            NewDataColumnName = Constants.AuditTableColumnNames.NewData,
            OperationTypeColumnName = Constants.AuditTableColumnNames.OperationType,
            UserColumnName = Constants.AuditTableColumnNames.User,
            TimestampColumnName = Constants.AuditTableColumnNames.Timestamp,
        };
    }

    private class CreateAuditTriggerSqlParameters
    {
        public string? TriggerName { get; set; }

        public string? AuditedEntityTableName { get; set; }

        public string? OperationType { get; set; }

        public string? AuditTableName { get; set; }

        public string? KeySource { get; set; }

        public string? OldRowDataSql { get; set; }

        public string? NewRowDataSql { get; set; }

        public string? KeyColumnName { get; set; }

        public string? OldDataColumnName { get; set; }

        public string? NewDataColumnName { get; set; }

        public string? OperationTypeColumnName { get; set; }

        public string? UserColumnName { get; set; }

        public string? TimestampColumnName { get; set; }
    }
}