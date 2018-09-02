using System;
using System.Diagnostics;
using System.IO;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;

namespace Pixel3D.LoopRecorder
{
	public class LoopManager<TDefinitions, TGameState>
	{
		// TODO should not be public
		public readonly Loop[] loopSlots;
		Value128 definitionHash;
		readonly DefinitionObjectTable definitionTable;

		int selectedLoop;
		// TODO should not be public
		public int? playingLoop;
		// TODO should not be public
		public int loopPlaybackPosition;

		TGameState gameState;

		public event Action<TGameState> OnGameStateReplaced;
		public event Func<bool> IsNetworked; 

		public LoopManager(int slots, TDefinitions definitions, TGameState gameState, Action<TGameState> onGameStateReplaced, Func<bool> isNetworked)
		{
			loopSlots = new Loop[slots];

			HashingStream hs = new HashingStream();
			BinaryWriter bw = new BinaryWriter(hs);

			SerializeContext serializeContext = new SerializeContext(bw, true);
			SerializeDefinitionFields(serializeContext, bw, definitions);
			definitionTable = serializeContext.GetAsDefinitionObjectTable();
			definitionHash = hs.GetHash();

			this.gameState = gameState;
			this.OnGameStateReplaced = onGameStateReplaced;
			this.IsNetworked = isNetworked;

			Trace.WriteLine("Definition Hash = " + definitionHash);
		}

		public bool IsPlaying => playingLoop.HasValue;

		public int SelectedLoop => selectedLoop;

		public void LoopStopAllRecording()
		{
			for (int i = 0; i < loopSlots.Length; i++)
			{
				if (loopSlots[i] != null)
					loopSlots[i].StopRecording();
			}
		}

		private void LoopStartPlaying(int loopToPlay)
		{
			if (loopSlots[loopToPlay] == null)
			{
				Trace.WriteLine("Loop slot " + loopToPlay + " is empty.");
				return;
			}

			if (loopSlots[loopToPlay].definitionHash != this.definitionHash)
			{
				Trace.WriteLine($"Loop slot {loopToPlay} has hash \"{loopSlots[loopToPlay].definitionHash}\", expected hash \"{this.definitionHash}\"");
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
				if(OnGameStateReplaced != null)
					OnGameStateReplaced(gameState);
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

				MultiInputState original = userInput;
				userInput = loopSlots[playingLoop.Value].inputFrames[loopPlaybackPosition++];
			}

			// Recording
			foreach (var loop in loopSlots)
			{
				if (loop != null && loop.IsRecording)
					loop.RecordInput(userInput);
			}

			// Send to game
			return userInput;
		}

		public void LoopPlayerUpdate(LoopCommand command, int? slotIndex = null)
		{
			if (command == 0)
				return;

			// Cannot use loop player while networked
			bool skipLoopsBecauseNetwork = false;
			if (IsNetworked != null && IsNetworked())
			{
				LoopStopAllRecording();
				LoopStopPlaying();
				skipLoopsBecauseNetwork = true;
			}

			if (command.HasFlag(LoopCommand.RecordHasFocus))
			{
				for (int i = 0; i < loopSlots.Length; i++)
				{
					if (slotIndex.HasValue && slotIndex.Value == i)
					{
						// Record the loop (can't record to the playing loop!)
						if (command.HasFlag(LoopCommand.Record) && !(playingLoop.HasValue && i == playingLoop.Value))
						{
							// If we were already recording on that slot, stop recording - this closes the file so we can re-open it!
							if (loopSlots[i] != null)
								loopSlots[i].StopRecording();

							byte[] saveState = Serialize();

							loopSlots[i] = Loop.StartRecording($"loop{i}.bin", saveState, definitionHash, "");
							if (command.HasFlag(LoopCommand.SnapshotOnly) || skipLoopsBecauseNetwork)
								loopSlots[i].StopRecording(); // <- just the snapshot
						}

						selectedLoop = i;
					}
				}
			}

			if (skipLoopsBecauseNetwork)
				return;

			if (droppedLoopFilename != null)
			{
				if (command.HasFlag(LoopCommand.NextLoop) || command.HasFlag(LoopCommand.PreviousLoop))
				{
					var dir = Path.GetDirectoryName(droppedLoopFilename);
					var files = Directory.GetFiles(dir, "*.bin", SearchOption.TopDirectoryOnly);
					Array.Sort(files, new NaturalStringComparer());
					int index = Array.IndexOf(files, droppedLoopFilename);
					if (index >= 0)
					{
						if (command == LoopCommand.NextLoop)
							index += 1;
						else
							index += (files.Length - 1);
						index %= files.Length;
						HandleDroppedLoop(files[index]);
					}
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
				if (loopSlots[selectedLoop] == null)
				{
					string filename = "loop" + selectedLoop + ".bin";
					if (File.Exists(filename))
						loopSlots[selectedLoop] = Loop.TryLoadFromFile(filename, ref definitionHash);
				}

				LoopStartPlaying(selectedLoop);
			}
		}

		private string droppedLoopFilename;

		public void SaveSnapshotOf(object gameState, string filename)
		{
			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);
			SerializeContext context = new SerializeContext(bw, false, definitionTable);
			Field.Serialize(context, bw, ref gameState);

			var loop = Loop.StartRecording(filename, ms.ToArray(), definitionHash, "");
			loop.StopRecording();
		}

		public void HandleDroppedLoop(string filename)
		{
			droppedLoopFilename = filename;

			try
			{
				using (FileStream fileStream = File.OpenRead(filename))
				{
					fileStream.Position = 0;

					Loop loop = Loop.TryLoadFromFile(filename, ref definitionHash);
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

					loopSlots[selectedLoop] = loop;
					LoopStartPlaying(selectedLoop);
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
				MultiInputState loopInput;
				if (loopPlaybackPosition >= loopSlots[playingLoop.Value].frameCount)
					loopInput = loopSlots[playingLoop.Value].inputFrames[0];
				else
					loopInput = loopSlots[playingLoop.Value].inputFrames[loopPlaybackPosition];

				return loopInput;
			}
			else
				return original;
		}

		#region Serialization 

		public void SerializeDefinitionFields(SerializeContext serializeContext, BinaryWriter bw, TDefinitions definitions)
		{
			Field.Serialize(serializeContext, bw, ref definitions);
		}

		public byte[] Serialize()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);

			var serializeContext = new SerializeContext(bw, false, definitionTable);
			Field.Serialize(serializeContext, bw, ref gameState);

			return ms.ToArray();
		}

		/// <summary>NOTE: Will throw an exception if the deserialization isn't clean (eg: bad network). Caller must handle.</summary>
		public void Deserialize(byte[] data)
		{
			MemoryStream ms = new MemoryStream(data);
			BinaryReader br = new BinaryReader(ms);

			var deserializeContext = new DeserializeContext(br, definitionTable);

			Field.Deserialize(deserializeContext, br, ref gameState);

			if (OnGameStateReplaced != null)
				OnGameStateReplaced(gameState);
		}

		public static TGameState SafeDeserialize(byte[] data, DefinitionObjectTable definitionTable)
		{
			MemoryStream ms = new MemoryStream(data);
			BinaryReader br = new BinaryReader(ms);

			var deserializeContext = new DeserializeContext(br, definitionTable);
			var result = default(TGameState);
			Field.Deserialize(deserializeContext, br, ref result);
			return result;
		}

		#endregion
	}
}
