// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Diagnostics;
using System.IO;

namespace Pixel3D.LoopRecorder
{
	public class LoopManager<TGameState>
	{
		private readonly object definitionTable;
		private readonly Loop[] loopSlots;
		private Value128 definitionHash;

		private string droppedLoopFilename;

		private TGameState gameState;
		private int loopPlaybackPosition;
		private int? playingLoop;

		public LoopManager(int slots,
			Value128 definitionHash,
			object definitionTable,
			TGameState gameState,
			Action<TGameState> onGameStateReplaced,
			Func<bool> isNetworked)
		{
			loopSlots = new Loop[slots];

			this.definitionHash = definitionHash;
			this.definitionTable = definitionTable;

			this.gameState = gameState;
			OnGameStateReplaced = onGameStateReplaced;
			IsNetworked = isNetworked;

			Trace.WriteLine("Definition Hash = " + definitionHash);
		}

		public bool IsPlaying => playingLoop.HasValue;
		public int SelectedLoop { get; private set; }

		public event Action<TGameState> OnGameStateReplaced;
		public event Func<bool> IsNetworked;

		public void LoopStopAllRecording()
		{
			for (var i = 0; i < loopSlots.Length; i++)
				if (loopSlots[i] != null)
					loopSlots[i].StopRecording();
		}

		private void LoopStartPlaying(int loopToPlay)
		{
			if (loopSlots[loopToPlay] == null)
			{
				Trace.WriteLine($"Loop slot {loopToPlay} is empty.");
				return;
			}

			if (loopSlots[loopToPlay].definitionHash != definitionHash)
			{
				Trace.WriteLine(
					$"Loop slot {loopToPlay} has hash \"{loopSlots[loopToPlay].definitionHash}\", expected hash \"{definitionHash}\"");
				return;
			}

			if (loopSlots[loopToPlay].saveState != null)
			{
				if (InjectSnapshot(loopSlots[loopToPlay].saveState))
				{
					loopPlaybackPosition = 0;
					if (loopSlots[loopToPlay].frameCount > 0) // Are we a loop or a snapshot
						playingLoop = loopToPlay;
					else
						playingLoop = null;
				}
				else
				{
					loopSlots[loopToPlay].saveState = null; // <- nuke the snapshot so we don't play it again
				}
			}
		}

		public void LoopStopPlaying()
		{
			playingLoop = null;
			loopPlaybackPosition = 0;
		}

		public bool InjectSnapshot(byte[] snapshot)
		{
			LoopStopAllRecording();
			LoopStopPlaying();

			TGameState loopGameState;
			try
			{
				// NOTE: This will throw if the game state doesn't deserialize cleanly
				//       (No guarantees about whether the deserialized data will explode once it starts simulating, though)
				loopGameState = SafeDeserialize(snapshot, definitionTable);
			}
			catch (Exception e)
			{
				// You probably modified a serializable structure, without a definition change (or something is *seriously* broken)
				Trace.WriteLine($"Error deserializing snapshot {e}");

				if (Debugger.IsAttached)
					Debugger.Break(); // Want to know when this happens (caller should have definition-checked)

				return false;
			}

			if (loopGameState != null)
			{
				gameState = loopGameState;
				OnGameStateReplaced?.Invoke(gameState);
				return true;
			}

			return false;
		}

		public MultiInputState LoopPlayerProcessInput(MultiInputState userInput)
		{
			// Playback
			if (playingLoop.HasValue)
			{
				if (loopPlaybackPosition >= loopSlots[playingLoop.Value].frameCount)
				{
					LoopStopAllRecording();
					Deserialize(loopSlots[playingLoop.Value].saveState);
					loopPlaybackPosition = 0;
				}

				var original = userInput;
				userInput = loopSlots[playingLoop.Value].inputFrames[loopPlaybackPosition++];
			}

			// Recording
			foreach (var loop in loopSlots)
				if (loop != null && loop.IsRecording)
					loop.RecordInput(userInput);

			// Send to game
			return userInput;
		}

		public void Update(LoopCommand command, int? slotIndex = null)
		{
			// Cannot use loop player while networked
			var skipLoopsBecauseNetwork = false;
			if (IsNetworked != null && IsNetworked())
			{
				LoopStopAllRecording();
				LoopStopPlaying();
				skipLoopsBecauseNetwork = true;
			}

			if (command.HasFlag(LoopCommand.RecordHasFocus))
				for (var i = 0; i < loopSlots.Length; i++)
					if (slotIndex.HasValue && slotIndex.Value == i)
					{
						// Record the loop (can't record to the playing loop!)
						if (command.HasFlag(LoopCommand.Record) && !(playingLoop.HasValue && i == playingLoop.Value))
						{
							// If we were already recording on that slot, stop recording - this closes the file so we can re-open it!
							if (loopSlots[i] != null)
								loopSlots[i].StopRecording();

							var saveState = Serialize();

							loopSlots[i] = Loop.StartRecording($"loop{i}.bin", saveState, definitionHash, "");
							if (command.HasFlag(LoopCommand.SnapshotOnly) || skipLoopsBecauseNetwork)
								loopSlots[i].StopRecording(); // <- just the snapshot
						}

						SelectedLoop = i;
					}

			if (skipLoopsBecauseNetwork)
				return;

			if (droppedLoopFilename != null)
				if (command.HasFlag(LoopCommand.NextLoop) || command.HasFlag(LoopCommand.PreviousLoop))
				{
					var dir = Path.GetDirectoryName(droppedLoopFilename);
					var files = Directory.GetFiles(dir, "*.bin", SearchOption.TopDirectoryOnly);
					Array.Sort(files, new NaturalStringComparer());
					var index = Array.IndexOf(files, droppedLoopFilename);
					if (index >= 0)
					{
						if (command == LoopCommand.NextLoop)
							index += 1;
						else
							index += files.Length - 1;
						index %= files.Length;
						HandleDroppedLoop(files[index]);
					}
				}

			if (command.HasFlag(LoopCommand.Stop))
			{
				LoopStopAllRecording();
				LoopStopPlaying();
			}

			if (command.HasFlag(LoopCommand.StartPlaying))
			{
				// Lazy-load from working directory:
				if (loopSlots[SelectedLoop] == null)
				{
					var filename = "loop" + SelectedLoop + ".bin";
					if (File.Exists(filename))
						loopSlots[SelectedLoop] = Loop.TryLoadFromFile(filename, ref definitionHash);
				}

				LoopStartPlaying(SelectedLoop);
			}
		}

		public void SaveSnapshotOf(TGameState gameState, string filename)
		{
			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);
			LoopSystem<TGameState>.serialize(bw, ref gameState, definitionTable);
			var loop = Loop.StartRecording(filename, ms.ToArray(), definitionHash, "");
			loop.StopRecording();
		}

		public void HandleDroppedLoop(string filename)
		{
			droppedLoopFilename = filename;

			try
			{
				using (var fileStream = File.OpenRead(filename))
				{
					fileStream.Position = 0;

					var loop = Loop.TryLoadFromFile(filename, ref definitionHash);
					if (loop == null)
					{
						Trace.WriteLine($"Failed to open file \"{filename}\"");
						return;
					}

					if (!loop.IsValid)
					{
						Trace.WriteLine($"Failed to open file \"{filename}\" (invalid)");
						return;
					}

					LoopStopAllRecording();
					LoopStopPlaying();

					loopSlots[SelectedLoop] = loop;
					LoopStartPlaying(SelectedLoop);
				}
			}
			catch (Exception e)
			{
				Trace.WriteLine($"Failed to open file \"{filename}\" ({e.Message})");
			}
		}

		public MultiInputState PreviewInput(MultiInputState original)
		{
			if (playingLoop.HasValue)
			{
				var loopInput = loopPlaybackPosition >= loopSlots[playingLoop.Value].frameCount
					? loopSlots[playingLoop.Value].inputFrames[0]
					: loopSlots[playingLoop.Value].inputFrames[loopPlaybackPosition];

				return loopInput;
			}

			return original;
		}

		#region Serialization 

		public byte[] Serialize()
		{
			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);
			LoopSystem<TGameState>.serialize(bw, ref gameState, definitionTable);
			return ms.ToArray();
		}

		/// <summary>NOTE: Will throw an exception if the deserialization isn't clean (eg: bad network). Caller must handle.</summary>
		public void Deserialize(byte[] data)
		{
			var ms = new MemoryStream(data);
			var br = new BinaryReader(ms);
			LoopSystem<TGameState>.deserialize(br, ref gameState, definitionTable);
			OnGameStateReplaced?.Invoke(gameState);
		}

		public static TGameState SafeDeserialize(byte[] data, object definitionTable)
		{
			var ms = new MemoryStream(data);
			var br = new BinaryReader(ms);

			var result = default(TGameState);
			LoopSystem<TGameState>.deserialize(br, ref result, definitionTable);
			return result;
		}

		#endregion
	}
}