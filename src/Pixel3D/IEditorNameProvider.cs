// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D
{
    /// <summary> Here specifically to support fetching the friendly names of untyped assets </summary>
    public interface IEditorNameProvider
    {
        string EditorName { get; }
    }
}