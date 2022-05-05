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
- _Key_: 0x1048 bytes from the DS ARM7 BIOS starting at 0x30.

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

The encryption is present in a cartridge only if the flag _Modcrypted_ is
present in the [cartridge header](header.md) [0x1C].

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

The key for the encryption is generated based on data of the cartridge. There
are two kinds of keys for developers / debugging and for retail software.

In both cases the initialization value (IV) is the same and depend in the
_modcrypt area_ (1 or 2) to apply the encryption:

- Area 1: First 0x10 bytes of the HMAC-SHA1 of the ARM9 program (with encrypted
  secure area). In the header at 0x300.
- Area 2: First 0x10 bytes of the HMAC-SHA1 of the ARM7 program. In the header
  at 0x314.

The IV and the key must be **reversed** before applying to the engine.

#### Debug software key

Use this key of the [cartridge header](header.md) has at least one of the
following flags enabled:

- DSi crypto flags ([0x1C]) with _ModcryptKeyDebug_.
- Program features ([0x1BF]) with _DeveloperApp_.

Then the 16-bytes of the key are the first 16 bytes of the cartridge (header).
This would be the _software short title_ and its _unique code_.

#### Retail key

The key is the result of scrambling two keys (X and Y) of 16 bytes.

- Key X:
  - 0..7: ASCII bytes of the word `Nintendo`.
  - 8..11: software unique code
  - 12..15: backward software unique code
- Key Y: First 16 bytes of the HMAC-SHA1 of the ARM9 DSi program.

Then use these two keys as a huge little-endian number so the final key is the
result of the first 16 bytes of the math expression:

```plain
key = ((keyX ^ keyY) + 0xFFFEFB4E295902582A680F5F1A4F3E79h) ROL 42
```

> [!NOTE]  
> The bitwise shift operation is _circular_. It moves the _overflown_ bits into
> the least significant position.

## Hashes

The cartridge contains several HMAC-SHA1 hashes that ensure the content of the
cartridge is not modified. These hashes are later included in the
[signature](#signature). Because of the signature, although they can be
re-generated, they can't be modified either.

### Cartridge header

The header contains most of the hashes. They were introduced starting the
[extended header](header.md#ds-extended-header) of DS games released after the
DSi. These hashes are identical to the three phases we can find inside the allow
list of DS games the DSi device contains. The allow list is only applied for
software that do not contain the hashes in the header.

All the cartridge header are standard HMAC-SHA1 hashes with different keys.

DS software specific hashes, from their header positions:

- 0x33C: Phase 3
  - Full banner content (including header, titles and icon).
  - The key is the same used for phase 3-4 allow list. Found in the launcher.
  - Only present if header field _extended program features_ has bit 6.
- 0x378: Phase 1.
  - First 0x160 bytes of header + ARM9 program with encrypted secure area +
    ARM7.
  - The key is the same used for phase 1-2 allow list. Found in launcher.
  - Only present if header field _extended program features_ has bit 6.
- 0x38C: Phase 2.
  - Overlay 9 table info + [FAT entries](filesystem.md#file-access-table) of the
    overlay files + partial content of the overlays
    - The hash may include the cartridge padding of the overlay (`0xFF` to 512
      bytes)
    - There is a maximum bytes to hash per overlay. The limit changes during the
      iteration.
    - The limit is `1 << 0xA` minus already overlay's blocks read divided by the
      remaining overlays to hash and finally multiplied by the block size (512
      bytes).
  - The key is the same used for phase 1-2 allow list. Found in launcher.
  - Only present if header field _extended program features_ has bit 6.

DSi software specific hashes, from their header positions:

- 0x300: ARM9 program with encrypted secure area.
- 0x314: ARM7 program.
- 0x328: Digest block area (only block area, no sector area hashes)
- 0x33C: Banner but using the DSi key this time.
- 0x350: ARM9 DSi program.
- 0x365: ARM7 DSi program.
- 0x3A0: ARM9 program skipping the secure area (first 16 KB).

All the DSi software hashes use the same key. It can be found in the ARM9
program, a 64-bytes block of data that starts with the _nitrocode_. It's the
second occurrence of the _nitrocode_.

> [!NOTE]  
> DSi software does not contain phase 1 and 2 hashes as DS software does. The
> digest area should cover and the additional hashes should cover the same.

### Overlays

Some DS software contains additional HMAC-SHA1 inside the ARM9 program for its
overlays. It seems these hashes are only verified by the program when it boots
in _download play_ mode (transferring the software to other console over the
air).

The location of these hashes inside the program is in the
[_ARM9 tail_](program.md#program-parameters). In contrast to the overlays hash
from the cartridge header, there is one hash per full content of the overlay.

The overlay info table has a flag per overlay indicating if the overlay has a
hash.

The 64-bytes key used to generate this hash is the one that starts with the
_nitrocode_. We can find this key inside the same ARM9 program at the second
occurrence of the _nitrocode_.

### Digest area

The digest are contains two subareas: sector hashes and block hashes.

The first area hashes almost the full content of the cartridge data. It creates
a set of hashes, one per _sector_. The length of the sector is defined in the
header at 0x200. The data to hash is at the same time divided in two areas: data
belonging to DS-compatible regions and DSi-specific regions. The start offset
and length of both areas are defined in the header (0x1E0). These areas must be
padded to the sector size (usually same as cartridge padding, 512 bytes).

The areas typically cover from the secure area (ARM9 program) to the last file
(just before the digest area) for the DS compatible region. The DSi-specific
region covers the ARM9 DSi and ARM7 DSi programs.

This list of hashes of the cartridge is then hashed again in blocks, creating
another list of hashes. Each block contains a number of hashes from the sector
area. The number of hashes is defined in the header at 0x204.

Finally, this last list of hashes is hashed one last time for the cartridge
header HMAC at 0x328.

Every hash of these sections, including the ones from the cartridge data are
HMAC-SHA1. The key is the same used for the DSi header HMACs.

> [!NOTE]  
> The hashes of the data corresponding to the ARM9 are for its encrypted secure
> area (KEY1), as it would be as we get it from the cartridge protocol.
>
> The hashes for the four programs (ARM9, ARM7, ARM9i, ARM7i) happen without
> modcrypt encryption (decrypted).

## CRC

The cartridge header also contains several CRC-16 checksums. The variant is
_MODBUS_.

## Signature

At the end of the cartridge header of DSi and late released DS games there is a
digital signature of the this header. It ensures that there is no information
about the loading process of the software is changed. It also covers the
[hashes](#hashes) so the rest of the cartridge cannot be modified either.

The signature is encrypted with the asymmetrical algorithm RSA. The private key
is unknown at this moment, so the signature cannot be re-generated. Custom
firmware like _unlaunch_ skip the verification of this signature, allowing
booting non-published games.

The public key can be generated from its two components:

- Exponent: constant `65537 (0x010001)`
- Public modulus: 128-bytes in the ARM9 BIOS at 0x8974

> [!NOTE]  
> You may need to prepend a 0 to the public modulus to ensure the software does
> not think it's a negative number.

The signature is the raw SHA-1 hash of the first 0xE00 bytes of the cartridge
(header) with PKCS #7 1.5 padding. **It is not encoded with ASN.1** as specified
in PKCS #1.

You can verify the signature by decrypting with RSA the value using the public
key and comparing with a SHA-1 hash of the header.

In DS cartridge is only present if header field _extended program features_ has
bit 6.
