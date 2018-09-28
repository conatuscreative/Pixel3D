// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.IO;

namespace Pixel3D.LoopRecorder
{
	public delegate void Serialize<TGameState>(BinaryWriter bw, ref TGameState gameState, object userData);
}