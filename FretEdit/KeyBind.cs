using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FretEdit {
	internal struct KeyBind {
		public KeyCode Value { get; set; }

		public void ReadConfig(Stream stream) {
			using (BinaryReader binaryReader = new BinaryReader(new NoCloseStream(stream))) {
				Value = (KeyCode)binaryReader.ReadInt32();
			}
		}

		public void WriteConfig(Stream stream) {
			using (BinaryWriter binaryWriter = new BinaryWriter(new NoCloseStream(stream))) {
				binaryWriter.Write((int)Value);
			}
		}
	}
}
