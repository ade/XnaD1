// Uncomment this to use extension methods instead (C# 3.0)
// You may also have to add references to System.Core
//
// Ie. instead of writing:
//
//     XnaSerialization.Write(buffer, myVector);
//
// ... you can simply write:
//
//     buffer.Write(myVector);
//
//#define DONT_USE_EXTENSION_METHODS

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Lidgren.Network.Xna
{
	public static class XnaSerialization
	{
#if DONT_USE_EXTENSION_METHODS

		/// <summary>
		/// Writes a Point
		/// </summary>
		public static void Write(NetBuffer buffer, Point pt)
		{
			buffer.Write(pt.X);
			buffer.Write(pt.Y);
		}

		/// <summary>
		/// Read a Point
		/// </summary>
		public static Point ReadPoint(NetBuffer buffer)
		{
			return new Point(buffer.ReadInt32(), buffer.ReadInt32());
		}

		/// <summary>
		/// Write a Single with half precision (16 bits)
		/// </summary>
		public static void WriteHalfPrecision(NetBuffer buffer, float value)
		{
			buffer.Write(new HalfSingle(value).PackedValue);
		}

		/// <summary>
		/// Reads a half precision Single written using WriteHalfPrecision(float)
		/// </summary>
		public static float ReadHalfPrecisionSingle(NetBuffer buffer)
		{
			HalfSingle h = new HalfSingle();
			h.PackedValue = buffer.ReadUInt16();
			return h.ToSingle();
		}

		/// <summary>
		/// Writes a Vector2
		/// </summary>
		public static void Write(NetBuffer buffer, Vector2 vector)
		{
			buffer.Write(vector.X);
			buffer.Write(vector.Y);
		}

		/// <summary>
		/// Reads a Vector2
		/// </summary>
		public static Vector2 ReadVector2(NetBuffer buffer)
		{
			Vector2 retval;
			retval.X = buffer.ReadSingle();
			retval.Y = buffer.ReadSingle();
			return retval;
		}

		/// <summary>
		/// Writes a Vector3
		/// </summary>
		public static void Write(NetBuffer buffer, Vector3 vector)
		{
			buffer.Write(vector.X);
			buffer.Write(vector.Y);
			buffer.Write(vector.Z);
		}

		/// <summary>
		/// Reads a Vector3
		/// </summary>
		public static Vector3 ReadVector3(NetBuffer buffer)
		{
			Vector3 retval;
			retval.X = buffer.ReadSingle();
			retval.Y = buffer.ReadSingle();
			retval.Z = buffer.ReadSingle();
			return retval;
		}

		/// <summary>
		/// Writes a Vector4
		/// </summary>
		public static void Write(NetBuffer buffer, Vector4 vector)
		{
			buffer.Write(vector.X);
			buffer.Write(vector.Y);
			buffer.Write(vector.Z);
			buffer.Write(vector.W);
		}

		/// <summary>
		/// Reads a Vector4
		/// </summary>
		public static Vector4 ReadVector4(NetBuffer buffer)
		{
			Vector4 retval;
			retval.X = buffer.ReadSingle();
			retval.Y = buffer.ReadSingle();
			retval.Z = buffer.ReadSingle();
			retval.W = buffer.ReadSingle();
			return retval;
		}


		/// <summary>
		/// Writes a unit vector (ie. a vector of length 1.0, for example a surface normal) 
		/// using specified number of bits
		/// </summary>
		public static void WriteUnitVector3(NetBuffer buffer, Vector3 unitVector, int numberOfBits)
		{
			float x = unitVector.X;
			float y = unitVector.Y;
			float z = unitVector.Z;
			double invPi = 1.0 / Math.PI;
			float phi = (float)(Math.Atan2(x, y) * invPi);
			float theta = (float)(Math.Atan2(z, Math.Sqrt(x * x + y * y)) * (invPi * 2));

			int halfBits = numberOfBits / 2;
			buffer.WriteSignedSingle(phi, halfBits);
			buffer.WriteSignedSingle(theta, numberOfBits - halfBits);
		}

		/// <summary>
		/// Reads a unit vector written using WriteUnitVector3(numberOfBits)
		/// </summary>
		public static Vector3 ReadUnitVector3(NetBuffer buffer, int numberOfBits)
		{
			int halfBits = numberOfBits / 2;
			float phi = buffer.ReadSignedSingle(halfBits) * (float)Math.PI;
			float theta = buffer.ReadSignedSingle(numberOfBits - halfBits) * (float)(Math.PI * 0.5);

			Vector3 retval;
			retval.X = (float)(Math.Sin(phi) * Math.Cos(theta));
			retval.Y = (float)(Math.Cos(phi) * Math.Cos(theta));
			retval.Z = (float)Math.Sin(theta);

			return retval;
		}

		/// <summary>
		/// Writes a unit quaternion using the specified number of bits per element
		/// for a total of 4 x bitsPerElements bits. Suggested value are:
		/// 8 bits = 4 bytes total, very low precision
		/// 12 bits = 6 bytes total, low precision
		/// 16 bits = 8 bytes total, average precision
		/// 20 bits = 10 bytes total, good precision
		/// 24 bits = 12 bytes total, full precision
		/// </summary>
		public static void WriteRotation(NetBuffer buffer, Quaternion quaternion, int bitsPerElement)
		{
			if (quaternion.X > 1.0f)
				quaternion.X = 1.0f;
			if (quaternion.Y > 1.0f)
				quaternion.Y = 1.0f;
			if (quaternion.Z > 1.0f)
				quaternion.Z = 1.0f;
			if (quaternion.W > 1.0f)
				quaternion.W = 1.0f;
			if (quaternion.X < -1.0f)
				quaternion.X = -1.0f;
			if (quaternion.Y < -1.0f)
				quaternion.Y = -1.0f;
			if (quaternion.Z < -1.0f)
				quaternion.Z = -1.0f;
			if (quaternion.W < -1.0f)
				quaternion.W = -1.0f;

			buffer.WriteSignedSingle(quaternion.X, bitsPerElement);
			buffer.WriteSignedSingle(quaternion.Y, bitsPerElement);
			buffer.WriteSignedSingle(quaternion.Z, bitsPerElement);
			buffer.WriteSignedSingle(quaternion.W, bitsPerElement);
		}

		/// <summary>
		/// Reads a unit quaternion written using WriteRotation(... ,bitsPerElement)
		/// </summary>
		public static Quaternion ReadRotation(NetBuffer buffer, int bitsPerElement)
		{
			Quaternion retval;
			retval.X = buffer.ReadSignedSingle(bitsPerElement);
			retval.Y = buffer.ReadSignedSingle(bitsPerElement);
			retval.Z = buffer.ReadSignedSingle(bitsPerElement);
			retval.W = buffer.ReadSignedSingle(bitsPerElement);
			return retval;
		}

		/// <summary>
		/// Writes an orthonormal matrix (rotation, translation but not scaling or projection)
		/// </summary>
		public static void WriteMatrix(NetBuffer buffer, ref Matrix matrix)
		{
			Quaternion rot = Quaternion.CreateFromRotationMatrix(matrix);
			WriteRotation(buffer, rot, 24);
			buffer.Write(matrix.M41);
			buffer.Write(matrix.M42);
			buffer.Write(matrix.M43);
		}

		/// <summary>
		/// Writes an orthonormal matrix (rotation, translation but no scaling or projection)
		/// </summary>
		public static void WriteMatrix(NetBuffer buffer, Matrix matrix)
		{
			Quaternion rot = Quaternion.CreateFromRotationMatrix(matrix);
			WriteRotation(buffer, rot, 24);
			buffer.Write(matrix.M41);
			buffer.Write(matrix.M42);
			buffer.Write(matrix.M43);
		}

		/// <summary>
		/// Reads a matrix written using WriteMatrix()
		/// </summary>
		public static Matrix ReadMatrix(NetBuffer buffer)
		{
			Quaternion rot = ReadRotation(buffer, 24);
			Matrix retval = Matrix.CreateFromQuaternion(rot);
			retval.M41 = buffer.ReadSingle();
			retval.M42 = buffer.ReadSingle();
			retval.M43 = buffer.ReadSingle();
			return retval;
		}

		/// <summary>
		/// Reads a matrix written using WriteMatrix()
		/// </summary>
		public static void ReadMatrix(NetBuffer buffer, ref Matrix destination)
		{
			Quaternion rot = ReadRotation(buffer, 24);
			destination = Matrix.CreateFromQuaternion(rot);
			destination.M41 = buffer.ReadSingle();
			destination.M42 = buffer.ReadSingle();
			destination.M43 = buffer.ReadSingle();
		}

		/// <summary>
		/// Writes a bounding sphere
		/// </summary>
		public static void Write(NetBuffer buffer, BoundingSphere bounds)
		{
			buffer.Write(bounds.Center.X);
			buffer.Write(bounds.Center.Y);
			buffer.Write(bounds.Center.Z);
			buffer.Write(bounds.Radius);
		}

		/// <summary>
		/// Reads a bounding sphere written using Write(buffer, BoundingSphere)
		/// </summary>
		public static BoundingSphere ReadBoundingSphere(NetBuffer buffer)
		{
			BoundingSphere retval;
			retval.Center.X = buffer.ReadSingle();
			retval.Center.Y = buffer.ReadSingle();
			retval.Center.Z = buffer.ReadSingle();
			retval.Radius = buffer.ReadSingle();
			return retval;
		}

#else

		/// <summary>
		/// Write a Point
		/// </summary>
		public static void Write(this NetBuffer buffer, Point value)
		{
			buffer.Write(value.X);
			buffer.Write(value.Y);
		}

		/// <summary>
		/// Read a Point
		/// </summary>
		public static Point ReadPoint(this NetBuffer buffer)
		{
			return new Point(buffer.ReadInt32(), buffer.ReadInt32());
		}
		
		/// <summary>
		/// Write a Single with half precision (16 bits)
		/// </summary>
		public static void WriteHalfPrecision(this NetBuffer buffer, float value)
		{
			buffer.Write(new HalfSingle(value).PackedValue);
		}

		/// <summary>
		/// Reads a half precision Single written using WriteHalfPrecision(float)
		/// </summary>
		public static float ReadHalfPrecisionSingle(this NetBuffer buffer)
		{
			HalfSingle h = new HalfSingle();
			h.PackedValue = buffer.ReadUInt16();
			return h.ToSingle();
		}

		/// <summary>
		/// Writes a Vector2
		/// </summary>
		public static void Write(this NetBuffer buffer, Vector2 vector)
		{
			buffer.Write(vector.X);
			buffer.Write(vector.Y);
		}

		/// <summary>
		/// Reads a Vector2
		/// </summary>
		public static Vector2 ReadVector2(this NetBuffer buffer)
		{
			Vector2 retval;
			retval.X = buffer.ReadSingle();
			retval.Y = buffer.ReadSingle();
			return retval;
		}

		/// <summary>
		/// Writes a Vector3
		/// </summary>
		public static void Write(this NetBuffer buffer, Vector3 vector)
		{
			buffer.Write(vector.X);
			buffer.Write(vector.Y);
			buffer.Write(vector.Z);
		}

		/// <summary>
		/// Writes a Vector3 at half precision
		/// </summary>
		public static void WriteHalfPrecision(this NetBuffer buffer, Vector3 vector)
		{
			buffer.Write(new HalfSingle(vector.X).PackedValue);
			buffer.Write(new HalfSingle(vector.Y).PackedValue);
			buffer.Write(new HalfSingle(vector.Z).PackedValue);
		}

		/// <summary>
		/// Reads a Vector3
		/// </summary>
		public static Vector3 ReadVector3(this NetBuffer buffer)
		{
			Vector3 retval;
			retval.X = buffer.ReadSingle();
			retval.Y = buffer.ReadSingle();
			retval.Z = buffer.ReadSingle();
			return retval;
		}

		/// <summary>
		/// Writes a Vector3 at half precision
		/// </summary>
		public static Vector3 ReadHalfPrecisionVector3(this NetBuffer buffer)
		{
			HalfSingle hx = new HalfSingle();
			hx.PackedValue = buffer.ReadUInt16();

			HalfSingle hy = new HalfSingle();
			hy.PackedValue = buffer.ReadUInt16();

			HalfSingle hz = new HalfSingle();
			hz.PackedValue = buffer.ReadUInt16();

			Vector3 retval;
			retval.X = hx.ToSingle();
			retval.Y = hy.ToSingle();
			retval.Z = hz.ToSingle();
			return retval;
		}

		/// <summary>
		/// Writes a Vector4
		/// </summary>
		public static void Write(this NetBuffer buffer, Vector4 vector)
		{
			buffer.Write(vector.X);
			buffer.Write(vector.Y);
			buffer.Write(vector.Z);
			buffer.Write(vector.W);
		}

		/// <summary>
		/// Reads a Vector4
		/// </summary>
		public static Vector4 ReadVector4(this NetBuffer buffer)
		{
			Vector4 retval;
			retval.X = buffer.ReadSingle();
			retval.Y = buffer.ReadSingle();
			retval.Z = buffer.ReadSingle();
			retval.W = buffer.ReadSingle();
			return retval;
		}


		/// <summary>
		/// Writes a unit vector (ie. a vector of length 1.0, for example a surface normal) 
		/// using specified number of bits
		/// </summary>
		public static void WriteUnitVector3(this NetBuffer buffer, Vector3 unitVector, int numberOfBits)
		{
			float x = unitVector.X;
			float y = unitVector.Y;
			float z = unitVector.Z;
			double invPi = 1.0 / Math.PI;
			float phi = (float)(Math.Atan2(x, y) * invPi);
			float theta = (float)(Math.Atan2(z, Math.Sqrt(x * x + y * y)) * (invPi * 2));

			int halfBits = numberOfBits / 2;
			buffer.WriteSignedSingle(phi, halfBits);
			buffer.WriteSignedSingle(theta, numberOfBits - halfBits);
		}

		/// <summary>
		/// Reads a unit vector written using WriteUnitVector3(numberOfBits)
		/// </summary>
		public static Vector3 ReadUnitVector3(this NetBuffer buffer, int numberOfBits)
		{
			int halfBits = numberOfBits / 2;
			float phi = buffer.ReadSignedSingle(halfBits) * (float)Math.PI;
			float theta = buffer.ReadSignedSingle(numberOfBits - halfBits) * (float)(Math.PI * 0.5);

			Vector3 retval;
			retval.X = (float)(Math.Sin(phi) * Math.Cos(theta));
			retval.Y = (float)(Math.Cos(phi) * Math.Cos(theta));
			retval.Z = (float)Math.Sin(theta);

			return retval;
		}

		/// <summary>
		/// Writes a unit quaternion using the specified number of bits per element
		/// for a total of 4 x bitsPerElements bits. Suggested value is 8 to 24 bits.
		/// </summary>
		public static void WriteRotation(this NetBuffer buffer, Quaternion quaternion, int bitsPerElement)
		{
			if (quaternion.X > 1.0f)
				quaternion.X = 1.0f;
			if (quaternion.Y > 1.0f)
				quaternion.Y = 1.0f;
			if (quaternion.Z > 1.0f)
				quaternion.Z = 1.0f;
			if (quaternion.W > 1.0f)
				quaternion.W = 1.0f;
			if (quaternion.X < -1.0f)
				quaternion.X = -1.0f;
			if (quaternion.Y < -1.0f)
				quaternion.Y = -1.0f;
			if (quaternion.Z < -1.0f)
				quaternion.Z = -1.0f;
			if (quaternion.W < -1.0f)
				quaternion.W = -1.0f;

			buffer.WriteSignedSingle(quaternion.X, bitsPerElement);
			buffer.WriteSignedSingle(quaternion.Y, bitsPerElement);
			buffer.WriteSignedSingle(quaternion.Z, bitsPerElement);
			buffer.WriteSignedSingle(quaternion.W, bitsPerElement);
		}

		/// <summary>
		/// Reads a unit quaternion written using WriteRotation(... ,bitsPerElement)
		/// </summary>
		public static Quaternion ReadRotation(this NetBuffer buffer, int bitsPerElement)
		{
			Quaternion retval;
			retval.X = buffer.ReadSignedSingle(bitsPerElement);
			retval.Y = buffer.ReadSignedSingle(bitsPerElement);
			retval.Z = buffer.ReadSignedSingle(bitsPerElement);
			retval.W = buffer.ReadSignedSingle(bitsPerElement);
			return retval;
		}

		/// <summary>
		/// Writes an orthonormal matrix (rotation, translation but not scaling or projection)
		/// </summary>
		public static void WriteMatrix(this NetBuffer buffer, ref Matrix matrix)
		{
			Quaternion rot = Quaternion.CreateFromRotationMatrix(matrix);
			WriteRotation(buffer, rot, 24);
			buffer.Write(matrix.M41);
			buffer.Write(matrix.M42);
			buffer.Write(matrix.M43);
		}

		/// <summary>
		/// Writes an orthonormal matrix (rotation, translation but no scaling or projection)
		/// </summary>
		public static void WriteMatrix(this NetBuffer buffer, Matrix matrix)
		{
			Quaternion rot = Quaternion.CreateFromRotationMatrix(matrix);
			WriteRotation(buffer, rot, 24);
			buffer.Write(matrix.M41);
			buffer.Write(matrix.M42);
			buffer.Write(matrix.M43);
		}

		/// <summary>
		/// Reads a matrix written using WriteMatrix()
		/// </summary>
		public static Matrix ReadMatrix(this NetBuffer buffer)
		{
			Quaternion rot = ReadRotation(buffer, 24);
			Matrix retval = Matrix.CreateFromQuaternion(rot);
			retval.M41 = buffer.ReadSingle();
			retval.M42 = buffer.ReadSingle();
			retval.M43 = buffer.ReadSingle();
			return retval;
		}

		/// <summary>
		/// Reads a matrix written using WriteMatrix()
		/// </summary>
		public static void ReadMatrix(this NetBuffer buffer, ref Matrix destination)
		{
			Quaternion rot = ReadRotation(buffer, 24);
			destination = Matrix.CreateFromQuaternion(rot);
			destination.M41 = buffer.ReadSingle();
			destination.M42 = buffer.ReadSingle();
			destination.M43 = buffer.ReadSingle();
		}

		/// <summary>
		/// Writes a bounding sphere
		/// </summary>
		public static void Write(this NetBuffer buffer, BoundingSphere bounds)
		{
			buffer.Write(bounds.Center.X);
			buffer.Write(bounds.Center.Y);
			buffer.Write(bounds.Center.Z);
			buffer.Write(bounds.Radius);
		}

		/// <summary>
		/// Reads a bounding sphere written using Write(buffer, BoundingSphere)
		/// </summary>
		public static BoundingSphere ReadBoundingSphere(this NetBuffer buffer)
		{
			BoundingSphere retval;
			retval.Center.X = buffer.ReadSingle();
			retval.Center.Y = buffer.ReadSingle();
			retval.Center.Z = buffer.ReadSingle();
			retval.Radius = buffer.ReadSingle();
			return retval;
		}
#endif
	}
}
