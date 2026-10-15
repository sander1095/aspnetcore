// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.OpenApi;

internal static class OpenApiSchemaExtensions
{
    private static readonly OpenApiSchema _nullSchema = new() { Type = JsonSchemaType.Null };

    public static IOpenApiSchema CreateOneOfNullableWrapper(this IOpenApiSchema originalSchema)
    {
        var wrapper = new OpenApiSchema
        {
            OneOf =
            [
                _nullSchema,
                originalSchema
            ]
        };

        // Move use-site annotations from the schema reference onto the wrapper because annotations in a oneOf branch
        // only apply when that branch matches (so they'd be lost for null values) and most tooling only reads them at the property level.
        if (originalSchema is OpenApiSchemaReference schemaReference)
        {
            var reference = schemaReference.Reference;
            wrapper.Description = reference.Description;
            wrapper.Deprecated = reference.Deprecated is true;
            wrapper.Default = reference.Default;
            wrapper.Examples = reference.Examples;

            reference.Description = null;
            reference.Deprecated = null;
            reference.Default = null;
            reference.Examples = null;
        }

        return wrapper;
    }

    public static void MakeArrayItemsNullable(this IOpenApiSchema schema)
    {
        if (schema is not OpenApiSchema { Items: { } items } arraySchema)
        {
            return;
        }

        if (items is OpenApiSchema { Type: { } itemType } inlineItemSchema)
        {
            inlineItemSchema.Type = itemType | JsonSchemaType.Null;
        }
        else
        {
            arraySchema.Items = items.CreateOneOfNullableWrapper();
        }
    }

    public static bool IsComponentizedSchema(this OpenApiSchema schema)
        => schema.IsComponentizedSchema(out _);

    public static bool IsComponentizedSchema(this OpenApiSchema schema, [NotNullWhen(true)] out string? schemaId)
    {
        if(schema.Metadata is not null
            && schema.Metadata.TryGetValue(OpenApiConstants.SchemaId, out var schemaIdAsObject)
            && schemaIdAsObject is string schemaIdString
            && !string.IsNullOrEmpty(schemaIdString))
        {
            schemaId = schemaIdString;
            return true;
        }
        schemaId = null;
        return false;
    }

    public static bool IsUnion(this OpenApiSchema schema)
        => schema.Metadata is not null
            && schema.Metadata.TryGetValue(OpenApiConstants.SchemaIsUnion, out var isUnion)
            && isUnion is true;
}
