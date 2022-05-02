# Cartridge security

## Blowfish (KEY1)

The cartridge data protocol data exchange as well as the cartridge secure area
are encrypted with _KEY1_ encryption. This is a variant of the the Blowfish
algorithm.

The algorithm works with two 32-bits blocks of data. It uses a key buffer of
`0x12 + 0x400` 32-bits.

### Initialization

The initialization takes as a parameter:

- _ID code_: for games it's the 4-chars game code.
- _Level_: depending on the data to encrypt/decrypt it's 1, 2 or 3.
- _Modulo_: for games it's `8`, for firmware it's `12`.
- _Key_: 64 bytes from the DS ARM7 BIOS.

```csharp
public void Initialize(uint idCode, int level, int modulo, uint[] key)
{
    // Initializes key buffer by copying the key (same length)
    Array.Copy(keyBuffer, key, 0);

    // Initializes key code from the ID code.
    uint[] keyCode = new uint[3] { idCode, idCode >> 1, idCode << 1 };

    ApplyKeyCode(modulo, keyCode); // level 1 (always)
    if (level >= 2) {
        ApplyKeyCode(modulo, keyCode);
    }

    keyCode[1] <<= 1;
    keyCode[2] >>= 1;

    if (level >= 3) {
        ApplyKeyCode(modulo, keyCode);
    }
}

private void ApplyKeyCode(int modulo, uint[] keyCode)
{
    Encrypt(ref keyCode[1], ref keyCode[2]);
    Encrypt(ref keyCode[0], ref keyCode[1]);

    modulo /= 4;
    for (int i = 0; i <= 0x11; i++) {
        uint xorValue = ReverseEndianness(keyCode[i % modulo]);
        keyBuffer[i] = keyBuffer[i] ^ xorValue;
    }

    uint scratch0 = 0;
    uint scratch1 = 0;
    for (int i = 0; i <= 0x410; i += 2) {
        Encrypt(ref scratch0, ref scratch1);
        keyBuffer[i] = scratch1;
        keyBuffer[i + 1] = scratch0;
    }
}
```

### Encryption or decryption

To decrypt or encrypt the algorithm is very similar, it only changes the
direction to iterate the bottom part of the key buffer.

```csharp
public void Encrypt(ref uint data0, ref uint data1)
{
    uint x = data1;
    uint y = data0;

    for (int i = 0; i < 0x10; i++) {
        uint z = keyBuffer[i] ^ x;
        x = GetMixer(z) ^ y;
        y = z;
    }

    data0 = x ^ keyBuffer[0x10];
    data1 = y ^ keyBuffer[0x11];
}

public void Decrypt(ref uint data0, ref uint data1)
{
    uint x = data1;
    uint y = data0;

    for (int i = 0x11; i >= 0x2; i--) {
        uint z = keyBuffer[i] ^ x;
        x = GetMixer(z) ^ y;
        y = z;
    }

    data0 = x ^ keyBuffer[0x01];
    data1 = y ^ keyBuffer[0x00];
}

private uint GetMixer(uint index)
{
    uint value = keyBuffer[0x12 + ((index >> 24) & 0xFF)];
    value += keyBuffer[0x112 + ((index >> 16) & 0xFF)];
    value ^= keyBuffer[0x212 + ((index >> 8) & 0xFF)];
    value += keyBuffer[0x312 + (index & 0xFF)];
    return value;
}
```

### Usage

The cartridge protocol use this encryption with different parameters as follow:

1. Decrypt "secure area disable" header value (0x78) with modulo 8 and level 1.
2. Initialize again with modulo 8 and level 2 to encrypt KEY1 commands and get
   the secure area.
3. Decrypt the first 8 bytes of the secure area (secure area ID)
4. Initialize again with modulo 8 and level 3 and decrypt the first 2 KB of the
   secure area
5. (DSi only) Initialize again with modulo 8 and level 1 to encrypt DSi KEY1
   commands.

> [!NOTE]  
> The _secure area ID_ is decrypted twice before getting its actual decrypted
> value. First it decrypts only its value with level 2 and then it's decrypted
> with the rest of the secure area with level 3.

## Modcrypt (AES-CTR)

DSi games may have some areas of the cartridge encrypted with an additional
encryption named _modcrypt_ which is a regular _AES-CTR (Counter)_.

### Implementation

You can implement this mode by using repeatedly AES-ECB with no padding mode.
The _counter_ mode works with a _counter register_, an array of 16 bytes.

![AES-CTR from Wikipedia](https://upload.wikimedia.org/wikipedia/commons/thumb/4/4d/CTR_encryption_2.svg/601px-CTR_encryption_2.svg.png)

Use the user provided _IV_ (initialization value) to set the initial value of
the counter. Then initialize the AES-ECB engine with a zero bytes _IV_
(initialization value) and the user provided key.

For each block of data, repeat the following operation:

1. Encrypt the current counter register into a new array of bytes, let's name it
   `xorMask`.
2. Read a block of data from the input.
3. Apply the bitwise operation XOR to each byte with one of the bytes from the
   `xorMask`. Iterate the `xorMask` from the end and the input from 0.
4. Write the result into the output
5. Increment the counter as the 16 bytes array were a big number in little
   endian.

### Key generation

## Hashes

### Cartridge header

### Overlays
