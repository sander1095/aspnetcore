// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Microsoft.AspNetCore.OpenApi;

internal static class OpenApiSchemaExtensions
{
    private static readonly OpenApiSchema _nullSchema = new() { Type = JsonSchemaType.Null };

    public static IOpenApiSchema CreateOneOfNullableWrapper(this IOpenApiSchema originalSchema)
    {
        return new OpenApiSchema
        {
            OneOf =
            [
                _nullSchema,
                originalSchema
            ]
        };
    }

    /// <summary>
    /// Applies the use-site annotations that were captured in the x-ref-* metadata of
    /// <paramref name="annotatedSchema"/> to <paramref name="useSiteSchema"/>.
    /// </summary>
    /// <param name="useSiteSchema">The schema that represents the use of a componentized schema: its schema reference, or the nullable wrapper around it.</param>
    /// <param name="annotatedSchema">The unresolved schema that carries the use-site annotations as metadata.</param>
    public static void ApplyReferenceAnnotations(this IOpenApiSchema useSiteSchema, OpenApiSchema annotatedSchema)
    {
        object? description = null;
        object? example = null;
        object? defaultAnnotation = null;
        object? deprecatedAnnotation = null;
        if (annotatedSchema.Metadata is not null)
        {
            annotatedSchema.Metadata.TryGetValue(OpenApiConstants.RefDescriptionAnnotation, out description);
            annotatedSchema.Metadata.TryGetValue(OpenApiConstants.RefExampleAnnotation, out example);
            annotatedSchema.Metadata.TryGetValue(OpenApiConstants.RefDefaultAnnotation, out defaultAnnotation);
            annotatedSchema.Metadata.TryGetValue(OpenApiConstants.RefDeprecatedAnnotation, out deprecatedAnnotation);
        }

        List<JsonNode>? examples = example is JsonNode exampleJson ? [exampleJson] : null;

        switch (useSiteSchema)
        {
            case OpenApiSchemaReference schemaReference:
                schemaReference.Description = description as string;
                schemaReference.Examples = examples;
                schemaReference.Default = defaultAnnotation as JsonNode;
                schemaReference.Deprecated = deprecatedAnnotation is true;
                break;
            case OpenApiSchema schema:
                schema.Description = description as string;
                schema.Examples = examples;
                schema.Default = defaultAnnotation as JsonNode;
                schema.Deprecated = deprecatedAnnotation is true;
                break;
        }
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
