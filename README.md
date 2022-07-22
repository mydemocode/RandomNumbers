**True random numbers generator tool**

Code for selecting a random number in the desired range with good statistical quality.
You need pregenerated binary random data files to use this, for example from TRNG services like www.random.org

## Usage

```C#

using TRNGTool;

var random_data_path = @"/my_binary_random_data_path";

var rgInt8  = new RandomNumbers<byte>(random_data_path, RandomFilesToRead.AllFiles, "*.bin");
var rgInt16 = new RandomNumbers<UInt16>(random_data_path, 10, "*.bin");
var rgInt32 = new RandomNumbers<UInt32>(random_data_path);

uint r1 = rgInt8.GetInt(0, 256);
uint r2 = rgInt16.GetInt(0, UInt16.MaxValue + 1);
uint r3 = rgInt32.GetInt(0, UInt32.MaxValue);
byte r4 = (byte)rgInt8.GetInt();
```