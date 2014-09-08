﻿using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Sodium
{
  /// <summary>Hashes passwords using the scrypt algorithm</summary>
  public class PasswordHash
  {
    private const uint SCRYPT_SALSA208_SHA256_BYTES = 102U;
    private const uint SCRYPT_SALSA208_SHA256_SALTBYTES = 32U;

    private const long OPSLIMIT_INTERACTIVE = 524288;
    private const long OPSLIMIT_MODERATE = 8388608;
    private const long OPSLIMIT_SENSITIVE = 33554432;

    private const int MEMLIMIT_INTERACTIVE = 16777216;
    private const int MEMLIMIT_MODERATE = 100000000;
    private const int MEMLIMIT_SENSITIVE = 1073741824;

    /// <summary>Represents predefined and useful limits for ScryptHashBinary() and ScryptHashString().</summary>
    public enum Strength
    {
      /// <summary>For interactive sessions (fast: uses 16MB of RAM).</summary>
      Interactive,
      /// <summary>For normal use (moderate: uses 100MB of RAM).</summary>
      Moderate,
      /// <summary>For highly sensitive data (slow: uses more than 1GB of RAM).</summary>
      Sensitive
    }

    /// <summary>Returns the hash in a string format, which includes the generated salt.</summary>
    /// <param name="password">The password.</param>
    /// <param name="limit">The limit for computation.</param>
    /// <returns>Returns an zero-terminated ASCII encoded string of the computed password and hash.</returns>
    public static string ScryptHashString(string password, Strength limit = Strength.Interactive)
    {
      int memLimit;
      long opsLimit;

      switch (limit)
      {
        case Strength.Interactive:
          opsLimit = OPSLIMIT_INTERACTIVE;
          memLimit = MEMLIMIT_INTERACTIVE;
          break;
        case Strength.Moderate:
          opsLimit = OPSLIMIT_MODERATE;
          memLimit = MEMLIMIT_MODERATE;
          break;
        case Strength.Sensitive:
          opsLimit = OPSLIMIT_SENSITIVE;
          memLimit = MEMLIMIT_SENSITIVE;
          break;
        default:
          opsLimit = OPSLIMIT_INTERACTIVE;
          memLimit = MEMLIMIT_INTERACTIVE;
          break;
      }

      return ScryptHashString(password, opsLimit, memLimit);
    }

    /// <summary>Returns the hash in a string format, which includes the generated salt.</summary>
    /// <param name="password">The password.</param>
    /// <param name="opsLimit">Represents a maximum amount of computations to perform.</param>
    /// <param name="memLimit">Is the maximum amount of RAM that the function will use, in bytes.</param>
    /// <returns>Returns an zero-terminated ASCII encoded string of the computed password and hash.</returns>
    public static string ScryptHashString(string password, long opsLimit, int memLimit)
    {
      if (password == null)
        throw new ArgumentNullException("password", "Password cannot be null");

      if (opsLimit <= 0)
        throw new ArgumentOutOfRangeException("opsLimit", "opsLimit cannot be zero or negative");

      if (memLimit <= 0)
        throw new ArgumentOutOfRangeException("memLimit", "memLimit cannot be zero or negative");

      var buffer = new byte[SCRYPT_SALSA208_SHA256_BYTES];
      var pass = Encoding.UTF8.GetBytes(password);

      var ret = SodiumCore.Is64 ? _HashString64(buffer, pass, pass.LongLength, opsLimit, memLimit) : 
        _HashString86(buffer, pass, pass.LongLength, opsLimit, memLimit);

      if (ret != 0)
      {
        throw new OutOfMemoryException("Internal error, hash failed");
      }

      return Encoding.UTF8.GetString(buffer);
    }

    /// <summary>Derives a secret key of any size from a password and a salt.</summary>
    /// <param name="password">The password.</param>
    /// <param name="salt">The salt.</param>
    /// <param name="limit">The limit for computation.</param>
    /// <param name="outputLength">The length of the computed output array.</param>
    /// <returns>Returns a byte array of the given size.</returns>
    public static byte[] ScryptHashBinary(string password, string salt, Strength limit = Strength.Interactive, long outputLength = SCRYPT_SALSA208_SHA256_SALTBYTES)
    {
      return ScryptHashBinary(Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes(salt), limit, outputLength);
    }

    /// <summary>Derives a secret key of any size from a password and a salt.</summary>
    /// <param name="password">The password.</param>
    /// <param name="salt">The salt.</param>
    /// <param name="limit">The limit for computation.</param>
    /// <param name="outputLength">The length of the computed output array.</param>
    /// <returns>Returns a byte array of the given size.</returns>
    public static byte[] ScryptHashBinary(byte[] password, byte[] salt, Strength limit = Strength.Interactive, long outputLength = SCRYPT_SALSA208_SHA256_SALTBYTES)
    {
      int memLimit;
      long opsLimit;

      switch (limit)
      {
        case Strength.Interactive:
          opsLimit = OPSLIMIT_INTERACTIVE;
          memLimit = MEMLIMIT_INTERACTIVE;
          break;
        case Strength.Moderate:
          opsLimit = OPSLIMIT_MODERATE;
          memLimit = MEMLIMIT_MODERATE;
          break;
        case Strength.Sensitive:
          opsLimit = OPSLIMIT_SENSITIVE;
          memLimit = MEMLIMIT_SENSITIVE;
          break;
        default:
          opsLimit = OPSLIMIT_INTERACTIVE;
          memLimit = MEMLIMIT_INTERACTIVE;
          break;
      }

      return ScryptHashBinary(password, salt, opsLimit, memLimit, outputLength);
    }

    /// <summary>
    /// Derives a secret key of any size from a password and a salt.
    /// </summary>
    /// <param name="password">The password.</param>
    /// <param name="salt">The salt.</param>
    /// <param name="opsLimit">Represents a maximum amount of computations to perform.</param>
    /// <param name="memLimit">Is the maximum amount of RAM that the function will use, in bytes.</param>
    /// <param name="outputLength">The length of the computed output array.</param>
    /// <returns>Returns a byte array of the given size.</returns>
    public static byte[] ScryptHashBinary(string password, string salt, long opsLimit, int memLimit, long outputLength = SCRYPT_SALSA208_SHA256_SALTBYTES)
    {
      var pass = Encoding.UTF8.GetBytes(password);
      var saltAsBytes = Encoding.UTF8.GetBytes(salt);

      return ScryptHashBinary(pass, saltAsBytes, opsLimit, memLimit, outputLength);
    }

    /// <summary>
    /// Derives a secret key of any size from a password and a salt.
    /// </summary>
    /// <param name="password">The password.</param>
    /// <param name="salt">The salt.</param>
    /// <param name="opsLimit">Represents a maximum amount of computations to perform.</param>
    /// <param name="memLimit">Is the maximum amount of RAM that the function will use, in bytes.</param>
    /// <param name="outputLength">The length of the computed output array.</param>
    /// <returns>Returns a byte array of the given size.</returns>
    public static byte[] ScryptHashBinary(byte[] password, byte[] salt, long opsLimit, int memLimit, long outputLength = SCRYPT_SALSA208_SHA256_SALTBYTES)
    {
      if (password == null)
        throw new ArgumentNullException("password", "Password cannot be null");

      if (salt == null)
        throw new ArgumentNullException("salt", "Salt cannot be null");

      if (salt.Length != SCRYPT_SALSA208_SHA256_SALTBYTES)
        throw new ArgumentOutOfRangeException("salt", "salt must be 32 bytes");

      if (opsLimit <= 0)
        throw new ArgumentOutOfRangeException("opsLimit", "opsLimit cannot be zero or negative");

      if (memLimit <= 0)
        throw new ArgumentOutOfRangeException("memLimit", "memLimit cannot be zero or negative");

      if (outputLength <= 0)
        throw new ArgumentOutOfRangeException("outputLength", "OutputLength cannot be zero or negative");

      var buffer = new byte[outputLength];

      var ret = SodiumCore.Is64 ? _HashBinary64(buffer, buffer.Length, password, password.LongLength, salt, opsLimit, memLimit) :
        _HashBinary86(buffer, buffer.Length, password, password.LongLength, salt, opsLimit, memLimit);

      if (ret != 0)
        throw new OutOfMemoryException("Internal error, hash failed");

      return buffer;
    }

    /// <summary>Verifies that a hash generated with ScryptHashString matches the supplied password.</summary>
    /// <param name="hash">The hash.</param>
    /// <param name="password">The password.</param>
    /// <returns><c>true</c> on success; otherwise, <c>false</c>.</returns>
    public static bool ScryptHashStringVerify(string hash, string password)
    {
      return ScryptHashStringVerify(Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(password));
    }

    /// <summary>Verifies that a hash generated with ScryptHashString matches the supplied password.</summary>
    /// <param name="hash">The hash.</param>
    /// <param name="password">The password.</param>
    /// <returns><c>true</c> on success; otherwise, <c>false</c>.</returns>
    public static bool ScryptHashStringVerify(byte[] hash, byte[] password)
    {
      if (password == null)
        throw new ArgumentNullException("password", "Password cannot be null");
      if (hash == null)
        throw new ArgumentNullException("hash", "Hash cannot be null");

      var ret = SodiumCore.Is64 ? _HashVerify64(hash, password, password.LongLength) : 
        _HashVerify86(hash, password, password.LongLength);

      return ret == 0;
    }

    //crypto_pwhash_scryptsalsa208sha256_str
    [DllImport(SodiumCore.LIBRARY_X64, EntryPoint = "crypto_pwhash_scryptsalsa208sha256_str", CallingConvention = CallingConvention.Cdecl)]
    private static extern int _HashString64(byte[] buffer, byte[] password, long passwordLen, long opsLimit, int memLimit);
    [DllImport(SodiumCore.LIBRARY_X86, EntryPoint = "crypto_pwhash_scryptsalsa208sha256_str", CallingConvention = CallingConvention.Cdecl)]
    private static extern int _HashString86(byte[] buffer, byte[] password, long passwordLen, long opsLimit, int memLimit);

    //crypto_pwhash_scryptsalsa208sha256
    [DllImport(SodiumCore.LIBRARY_X64, EntryPoint = "crypto_pwhash_scryptsalsa208sha256", CallingConvention = CallingConvention.Cdecl)]
    private static extern int _HashBinary64(byte[] buffer, long bufferLen, byte[] password, long passwordLen, byte[] salt, long opsLimit, int memLimit);
    [DllImport(SodiumCore.LIBRARY_X86, EntryPoint = "crypto_pwhash_scryptsalsa208sha256", CallingConvention = CallingConvention.Cdecl)]
    private static extern int _HashBinary86(byte[] buffer, long bufferLen, byte[] password, long passwordLen, byte[] salt, long opsLimit, int memLimit);

    //crypto_pwhash_scryptsalsa208sha256_str_verify
    [DllImport(SodiumCore.LIBRARY_X86, EntryPoint = "crypto_pwhash_scryptsalsa208sha256_str_verify", CallingConvention = CallingConvention.Cdecl)]
    private static extern int _HashVerify86(byte[] buffer, byte[] password, long passLength);
    [DllImport(SodiumCore.LIBRARY_X64, EntryPoint = "crypto_pwhash_scryptsalsa208sha256_str_verify", CallingConvention = CallingConvention.Cdecl)]
    private static extern int _HashVerify64(byte[] buffer, byte[] password, long passLength);
  }
}
