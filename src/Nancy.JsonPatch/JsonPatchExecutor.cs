﻿using System;
using System.Collections.Generic;
using Nancy.JsonPatch.Models;
using Nancy.JsonPatch.OperationProcessor;
using Nancy.JsonPatch.PathParser;
using Nancy.JsonPatch.PropertyResolver;

namespace Nancy.JsonPatch
{
    public class JsonPatchExecutor
    {
        public JsonPatchResult Patch<T>(string requestBody, T target)
        {
            return Patch(requestBody, target, new JsonPatchPropertyResolver());
        }

        public JsonPatchResult Patch<T>(string requestBody, T target, IJsonPatchPropertyResolver propertyResolver)
        {
            var documentParser = new DocumentParser.JsonPatchDocumentParser();
            
            List<JsonPatchOperation> operations;
            try
            {
                operations = documentParser.DeserializeJsonPatchRequest(requestBody);
            }
            catch (Exception ex)
            {
                return Failure(JsonPatchFailureReason.CouldNotParseJson, ex.Message);
            }

            return PatchUsing(target, operations);
        }

        public JsonPatchResult PatchUsing<T>(T target, IEnumerable<JsonPatchOperation> operations)
        {
            var pathParser = new JsonPatchPathParser(new JsonPatchPropertyResolver());
            var operationExecutor = new JsonPatchOperationExecutor();

            foreach (var operation in operations)
            {
                var pathResult = pathParser.ParsePath(operation.Path, target);
                if (pathResult.Path == null)
                {
                    return Failure(JsonPatchFailureReason.CouldNotParsePath, pathResult.Error);
                }

                switch (operation.Op)
                {
                    case JsonPatchOpCode.replace:
                        var replaceResult = operationExecutor.Replace(pathResult.Path, operation.Value);
                        if (!replaceResult.Succeeded)
                        {
                            return Failure(JsonPatchFailureReason.OperationFailed, replaceResult.Message);
                        }
                        break;

                    case JsonPatchOpCode.move:
                        var moveFrom = pathParser.ParsePath(operation.From, target);
                        if (moveFrom.Path == null)
                        {
                            return Failure(JsonPatchFailureReason.CouldNotParseFrom, moveFrom.Error);
                        }

                        var moveResult = operationExecutor.Move(moveFrom.Path, pathResult.Path);
                        if (!moveResult.Succeeded)
                        {
                            return Failure(JsonPatchFailureReason.OperationFailed, moveResult.Message);                            
                        }
                        break;

                    case JsonPatchOpCode.copy:
                        var copyFrom = pathParser.ParsePath(operation.From, target);
                        if (copyFrom.Path == null)
                        {
                            return Failure(JsonPatchFailureReason.CouldNotParseFrom, copyFrom.Error);
                        }

                        var copyResult = operationExecutor.Copy(copyFrom.Path, pathResult.Path);
                        if (!copyResult.Succeeded)
                        {
                            return Failure(JsonPatchFailureReason.OperationFailed, copyResult.Message);                            
                        }
                        break;

                    case JsonPatchOpCode.add:
                        var addResult = operationExecutor.Add(pathResult.Path, operation.Value);
                        if (!addResult.Succeeded)
                        {
                            return Failure(JsonPatchFailureReason.OperationFailed, addResult.Message);
                        }
                        break;

                    case JsonPatchOpCode.remove:
                        var removeResult = operationExecutor.Remove(pathResult.Path);
                        if (!removeResult.Succeeded)
                        {
                            return Failure(JsonPatchFailureReason.OperationFailed, removeResult.Message);
                        }
                        break;

                    case JsonPatchOpCode.test:
                        var result = operationExecutor.Test(pathResult.Path, operation.Value);
                        if (!result.Succeeded)
                        {
                            return Failure(JsonPatchFailureReason.TestFailed, result.Message);
                        }
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
            return new JsonPatchResult {Succeeded = true};
        }

        private static JsonPatchResult Failure(JsonPatchFailureReason reason, string error)
        {
            return new JsonPatchResult
            {
                FailureReason = reason,
                Succeeded = false,
                Message = error
            };
        }
    }
}
