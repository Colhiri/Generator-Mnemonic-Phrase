using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Generator_Mnemonics
{
    public enum WordListLanguage
    {
        ChineseSimplified,
        ChineseTraditional,
        English,
        French,
        Italian,
        Japanese,
        Korean,
        Spanish
    }
    class Program
    {
        private static string[] _english;
        private static string[] lang;
        private static string[] _chineseSimplified;
        private static string[] _chineseTraditional;
        private static string[] _french;
        private static string[] _italian;
        private static string[] _japanese;
        private static string[] _korean;
        private static string[] _spanish;
        private const string InvalidMnemonic = "Invalid mnemonic";
        private const string InvalidEntropy = "Invalid entropy";
        private const string InvalidChecksum = "Invalid mnemonic checksum";
        private const string salt = "mnemonic";
        private const string bitcoinSeed = "Bitcoin seed";
        private static string PATH1, filePath, language;
        private static string prefix_legacy = "00";
        private static string prefix_segwit = "05";
        private static string hardened = "";
        private static List<string> PATH = new List<string>();
        private static readonly BigInteger order = BigInteger.Parse("115792089237316195423570985008687907852837564279074904382605163141518161494337");
        private static long Total, Found = 0;
        private static BigInteger IncrementalSearch, Entropy = 0;
        private static BigInteger Step = 1, EntropyStep = 1;
        private static bool Silent = true;
        private static int processorCount = Environment.ProcessorCount;
        private static int num = 1;
        private static int words = 12;
        private static long derivation = 0;
        private static int mode = 1;
        private static WordListLanguage Language_list;
        private static bool IsRunning = true;
        private static HashSet<string> _addressDb;
        private static double cur, speed, Constant;
        private static Stopwatch sw = new Stopwatch();
        private static readonly object outFileLock = new();
        private static bool IncrementalEntropy = false;
        private static string EntropyLength;


        //static DateTime t1, t2;

        static void Main(string[] args)
        {


        }

        
        private static void WorkerThread()
        {
            Console.OutputEncoding = Encoding.UTF8;
            //int processorCount = Environment.ProcessorCount;
            //long Total = 0;
            InitializationWordList();
            //var lang = _english;
            //var Language_list = WordListLanguage.ChineseTraditional;
            switch (language)
            {
                case "EN":
                    {
                        lang = _english;
                        Language_list = WordListLanguage.English;
                        break;
                    }
                case "CS":
                    {
                        lang = _chineseSimplified;
                        Language_list = WordListLanguage.ChineseSimplified;
                        break;
                    }
                case "CT":
                    {
                        lang = _chineseTraditional;
                        Language_list = WordListLanguage.ChineseTraditional;
                        break;
                    }
                case "FR":
                    {
                        lang = _french;
                        Language_list = WordListLanguage.French;
                        break;
                    }
                case "IT":
                    {
                        lang = _italian;
                        Language_list = WordListLanguage.Italian;
                        break;
                    }
                case "JA":
                    {
                        lang = _japanese;
                        Language_list = WordListLanguage.Japanese;
                        break;
                    }
                case "KO":
                    {
                        lang = _korean;
                        Language_list = WordListLanguage.Korean;
                        break;
                    }
                case "SP":
                    {
                        lang = _spanish;
                        Language_list = WordListLanguage.Spanish;
                        break;
                    }
                default:
                    {
                        lang = _english;
                        Language_list = WordListLanguage.English;
                        break;
                    }
            }
            sw.Start();
            while (true)
            {
                //Шаг 1. Получаем энтропию.
                //var seedBytes = GenerateMnemonicBytes(words);
                byte[] seedBytes;
                if (IncrementalEntropy)
                {
                    //Console.WriteLine("Entropy: " + Entropy.ToString(EntropyLength));
                    seedBytes = StringToByteArray(Entropy.ToString(EntropyLength));
                    
                    Entropy = Entropy + EntropyStep;
                }
                else
                {
                    seedBytes = GenerateMnemonicBytes(words);
                }
                //var lang = _english;
                //var Language_list = WordListLanguage.ChineseTraditional;
                //var seed = EntropyToMnemonic(seedBytes, _english, WordListLanguage.English);
                var seed = EntropyToMnemonic(seedBytes, lang, Language_list);
                //var seed = "edge shift acquire essence sniff ankle ten prevent december drama churn feel shed ring pair curve biology ability equal cherry yellow blush abuse drift";
                //Console.WriteLine($"Seed: {seed}");
                var saltByte = Encoding.UTF8.GetBytes(salt);
                var masterSecret = Encoding.UTF8.GetBytes(bitcoinSeed);
                var BIP39SeedByte = new Rfc2898DeriveBytes(seed, saltByte, 2048, HashAlgorithmName.SHA512).GetBytes(64);
                //var BIP39Seed = BytesToHexString(BIP39SeedByte);
                var masterPrivateKey = new byte[32]; // Master private key
                var masterChainCode = new byte[32]; // Master chain code
                //Console.WriteLine($"BIP39Seed: {BIP39Seed}");
                var hmac = new HMACSHA512(masterSecret);
                var i = hmac.ComputeHash(BIP39SeedByte);
                Buffer.BlockCopy(i, 0, masterPrivateKey, 0, 32);
                Buffer.BlockCopy(i, 32, masterChainCode, 0, 32);
                //Console.WriteLine($"Master Private Key: {BytesToHexString(masterPrivateKey)}");
                //Console.WriteLine($"Master Chain Code: {BytesToHexString(masterChainCode)}");
                foreach (var path in PATH)
                {
                    int a = 0;
                    while (a <= derivation)
                    {
                        string DER_PATH = path + '/' + a + hardened;
                        //Console.WriteLine(DER_PATH);
                        byte[] PrivateKey = GetChildKey(masterPrivateKey, masterChainCode, DER_PATH);
                        var range = BytesToHexString(PrivateKey).Length;
                        if (range != 64)
                        {
                            while (range != 64)
                            {
                                //Console.WriteLine("D!");
                                string PVK64 = BytesToHexString(PrivateKey);
                                //Console.WriteLine(PVK64);
                                PrivateKey = StringToByteArray("00" + PVK64);
                                //Console.WriteLine("Проверка ключа ппосле добвки нулей " + BytesToHexString(PrivateKey));
                                range++;
                                range++;
                            }
                        }
                        if (IncrementalSearch != 0)
                        {
                            long n = 0;
                            BigInteger PrivateDEC = BigInteger.Parse(BytesToHexString(PrivateKey), System.Globalization.NumberStyles.AllowHexSpecifier);
                            while (n <= IncrementalSearch)
                            {
                                //Считаем Приватник + N число
                                var PrivateDEC1 = PrivateDEC - (n * Step);
                                var PrivateDEC0 = PrivateDEC + (n * Step);
                                //Console.WriteLine(PrivateDEC.ToString("X32"));
                                var PrivateHEX1 = PrivateDEC1.ToString("X64");
                                var PrivateHEX0 = PrivateDEC0.ToString("X64");
                                
                                //Console.WriteLine(PrivateDEC0.ToString("X32"));
                                //Console.WriteLine(PrivateDEC1.ToString("X32"));
                                //range = PrivateDEC.ToString("X32").Length;
                                //if (range < 64)
                                //{
                                //    while (range < 64)
                                //    {
                                //        PrivateHEX0 = '0' + PrivateHEX0;
                                //        PrivateHEX1 = '0' + PrivateHEX1;
                                //        range++;
                                //    }
                                //}
                                //else if (range > 64)
                                //{
                                //    PrivateHEX0 = PrivateHEX0[^64..];
                                //    PrivateHEX1 = PrivateHEX1[^64..];
                                //}
                                var PrivateKeyCHILD = StringToByteArray(PrivateHEX0);
                                var Public_key_compressed = Secp256K1Manager.GetPublicKey(PrivateKeyCHILD, true);
                                var Public_key_uncompressed = Secp256K1Manager.GetPublicKey(PrivateKeyCHILD, false);
                                var address_compressed = GetAddress(Public_key_compressed);
                                var address_uncompressed = GetAddress(Public_key_uncompressed);
                                //var address_compressed = GetHash160(Public_key_compressed);
                                //var address_uncompressed = GetHash160(Public_key_uncompressed);
                                
                                //var eth_adr = GetEthAddress(Public_key_uncompressed);
                                Interlocked.Increment(ref Total);
                                string address_segwit;
                                bool flag1;
                                if (prefix_segwit == "")
                                {
                                    address_segwit = "NULL";
                                    flag1 = false;
                                }
                                else
                                {
                                    address_segwit = GetSegWit_base58(Public_key_compressed);
                                    flag1 = HasBalance(address_segwit);
                                }
                                bool flag = HasBalance(address_compressed);
                                bool flag0 = HasBalance(address_uncompressed);
                                

                                if ((flag != false) || (flag0 != false) || (flag1 != false))
                                {
                                    Console.WriteLine(address_compressed + "\n" + address_uncompressed + "\n" + address_segwit + "\nmnemonic: " + seed + "\nPrivate Key: " + BytesToHexString(PrivateKeyCHILD) + "   N = +" + (n*Step).ToString("X8") + "\nEntropy: " + BytesToHexString(seedBytes) + "\nDer.PATH: " + DER_PATH + "\nTotal: " + Total + " Found: " + Found + " Speed: " + speed + '\n');
                                    string contents = string.Format($" \n\nAddresses: \n{address_compressed}  Balance: {flag}  \n{address_uncompressed}  Balance: {flag0}  \n{address_segwit}  :  Balance: {flag1} \nMnemonic phrase: {seed} \nPrivate Key: {BytesToHexString(PrivateKeyCHILD)} \nEntropy: {BytesToHexString(seedBytes)}  \nDerivation PATH: {DER_PATH}");
                                    object outFileLock = Program.outFileLock;
                                    bool lockTaken = false;
                                    try
                                    {
                                        Monitor.Enter(outFileLock, ref lockTaken);
                                        File.AppendAllText("FOUND.txt", contents);
                                    }
                                    finally
                                    {
                                        if (lockTaken)
                                            Monitor.Exit(outFileLock);
                                    }
                                    Interlocked.Increment(ref Found);

                                }
                                else
                                {
                                    if (Silent == false)
                                    {
                                        double Elapsed_MS = sw.ElapsedTicks;
                                        cur = Elapsed_MS / Constant;
                                        cur = Math.Round(cur);
                                        speed = Total / (Elapsed_MS / Constant);
                                        speed = Math.Round(speed);
                                        Console.WriteLine(address_compressed + "\n" + address_uncompressed + "\n" + address_segwit + "\nmnemonic: " + seed + "\nPrivate Key: " + BytesToHexString(PrivateKeyCHILD) + "   N = +" + (n*Step).ToString("X8") + "\nEntropy: " + BytesToHexString(seedBytes) + "\nDer.PATH: " + DER_PATH + "\nTotal: " + Total + " Found: " + Found + " Speed: " + speed + '\n');

                                    }
                                }

                                //Считаем Приватник - N число
                                PrivateKeyCHILD = StringToByteArray(PrivateHEX1);
                                Public_key_compressed = Secp256K1Manager.GetPublicKey(PrivateKeyCHILD, true);
                                Public_key_uncompressed = Secp256K1Manager.GetPublicKey(PrivateKeyCHILD, false);
                                address_compressed = GetAddress(Public_key_compressed);
                                address_uncompressed = GetAddress(Public_key_uncompressed);
                                //var address_compressed = GetHash160(Public_key_compressed);
                                //var address_uncompressed = GetHash160(Public_key_uncompressed);
                                //address_segwit = GetSegWit_base58(Public_key_compressed);
                                //var eth_adr = GetEthAddress(Public_key_uncompressed);
                                Interlocked.Increment(ref Total);
                                if (prefix_segwit == "")
                                {
                                    address_segwit = "NULL";
                                    flag1 = false;
                                }
                                else
                                {
                                    address_segwit = GetSegWit_base58(Public_key_compressed);
                                    flag1 = HasBalance(address_segwit);
                                }

                                flag = HasBalance(address_compressed);
                                flag0 = HasBalance(address_uncompressed);
                                //flag1 = HasBalance(address_segwit);

                                if ((flag != false) || (flag0 != false) || (flag1 != false))
                                {
                                    Console.WriteLine(address_compressed + "\n" + address_uncompressed + "\n" + address_segwit + "\nmnemonic: " + seed + "\nPrivate Key: " + BytesToHexString(PrivateKeyCHILD) + "   N = -" + (n*Step).ToString("X8") + "\nEntropy: " + BytesToHexString(seedBytes) + "\nDer.PATH: " + DER_PATH + "\nTotal: " + Total + " Found: " + Found + " Speed: " + speed + '\n');
                                    string contents = string.Format($" \n\nAddresses: \n{address_compressed}  Balance: {flag}  \n{address_uncompressed}  Balance: {flag0}  \n{address_segwit}  :  Balance: {flag1} \nMnemonic phrase: {seed} \nPrivate Key: {BytesToHexString(PrivateKeyCHILD)}  \nEntropy: {BytesToHexString(seedBytes)} \nDerivation PATH: {DER_PATH}");
                                    object outFileLock = Program.outFileLock;
                                    bool lockTaken = false;
                                    try
                                    {
                                        Monitor.Enter(outFileLock, ref lockTaken);
                                        File.AppendAllText("FOUND.txt", contents);
                                    }
                                    finally
                                    {
                                        if (lockTaken)
                                            Monitor.Exit(outFileLock);
                                    }
                                    Interlocked.Increment(ref Found);

                                }
                                else
                                {
                                    if (Silent == false)
                                    {
                                        double Elapsed_MS = sw.ElapsedTicks;
                                        cur = Elapsed_MS / Constant;
                                        cur = Math.Round(cur);
                                        speed = Total / (Elapsed_MS / Constant);
                                        speed = Math.Round(speed);
                                        Console.WriteLine(address_compressed + "\n" + address_uncompressed + "\n" + address_segwit + "\nmnemonic: " + seed + "\nPrivate Key: " + BytesToHexString(PrivateKeyCHILD) + "   N = -" + (n*Step).ToString("X8") + "\nEntropy: " + BytesToHexString(seedBytes) + "\nDer.PATH: " + DER_PATH + "\nTotal: " + Total + " Found: " + Found + " Speed: " + speed + '\n');

                                    }
                                }
                                n++;
                            }
                        }
                        else
                        {
                            var Public_key_compressed = Secp256K1Manager.GetPublicKey(PrivateKey, true);
                            var Public_key_uncompressed = Secp256K1Manager.GetPublicKey(PrivateKey, false);



                            var address_compressed = GetAddress(Public_key_compressed);
                            var address_uncompressed = GetAddress(Public_key_uncompressed);
                            //var address_compressed = GetHash160(Public_key_compressed);
                            //var address_uncompressed = GetHash160(Public_key_uncompressed);
                            //var address_segwit = GetSegWit_base58(Public_key_compressed);
                            //var eth_adr = GetEthAddress(Public_key_uncompressed);
                            Interlocked.Increment(ref Total);
                            string address_segwit;
                            bool flag1;
                            if (prefix_segwit == "")
                            {
                                address_segwit = "NULL";
                                flag1 = false;
                            }
                            else
                            {
                                address_segwit = GetSegWit_base58(Public_key_compressed);
                                flag1 = HasBalance(address_segwit);
                            }

                            bool flag = HasBalance(address_compressed);
                            bool flag0 = HasBalance(address_uncompressed);
                            //bool flag1 = HasBalance(address_segwit);

                            if ((flag != false) || (flag0 != false) || (flag1 != false))
                            {
                                Console.WriteLine(address_compressed + "\n" + address_uncompressed + "\n" + address_segwit + "\nmnemonic: " + seed + "\nPrivate Key: " + BytesToHexString(PrivateKey) + "   N: " + "\nEntropy: " + BytesToHexString(seedBytes) + "\nDer.PATH: " + DER_PATH + "\nTotal: " + Total + " Found: " + Found + " Speed: " + speed + '\n');
                                string contents = string.Format($" \n\nAddresses: \n{address_compressed}  Balance: {flag}  \n{address_uncompressed}  Balance: {flag0}  \n{address_segwit}  :  Balance: {flag1} \nMnemonic phrase: {seed} \nPrivate Key: {BytesToHexString(PrivateKey)}  \nEntropy: {BytesToHexString(seedBytes)} \nDerivation PATH: {DER_PATH}");
                                object outFileLock = Program.outFileLock;
                                bool lockTaken = false;
                                try
                                {
                                    Monitor.Enter(outFileLock, ref lockTaken);
                                    File.AppendAllText("FOUND.txt", contents);
                                }
                                finally
                                {
                                    if (lockTaken)
                                        Monitor.Exit(outFileLock);
                                }
                                Interlocked.Increment(ref Found);

                            }
                            else
                            {
                                if (Silent == false)
                                {
                                    double Elapsed_MS = sw.ElapsedTicks;
                                    cur = Elapsed_MS / Constant;
                                    cur = Math.Round(cur);
                                    speed = Total / (Elapsed_MS / Constant);
                                    speed = Math.Round(speed);
                                    Console.WriteLine(address_compressed + "\n" + address_uncompressed + "\n" + address_segwit + "\nmnemonic: " + seed + "\nPrivate Key: " + BytesToHexString(PrivateKey) + "\nEntropy: " + BytesToHexString(seedBytes) + "\nDer.PATH: " + DER_PATH + "\nTotal: " + Total + " Found: " + Found + " Speed: " + speed + '\n');

                                }
                            }
                        }
                        a++;
                    }
                }
            }
        }

        private static HashSet<string> LoadDatabase(string filePath)
        {
            HashSet<string> stringSet = new();
            foreach (string readLine in File.ReadLines(filePath))
            {
                string[] strArray = readLine.Split('\t');
                stringSet.Add(strArray[0]);
            }
            return stringSet;
        }

        private static bool HasBalance(string address) => _addressDb.Contains(address);

        private static void InitializationWordList()
        {
            _english = GetWordList(WordListLanguage.English);
            _chineseSimplified = GetWordList(WordListLanguage.ChineseSimplified);
            _chineseTraditional = GetWordList(WordListLanguage.ChineseTraditional);
            _french = GetWordList(WordListLanguage.French);
            _italian = GetWordList(WordListLanguage.Italian);
            _japanese = GetWordList(WordListLanguage.Japanese);
            _korean = GetWordList(WordListLanguage.Korean);
            _spanish = GetWordList(WordListLanguage.Spanish);
        }

        private static byte[] GetChildKey(byte[] Private, byte[] Chain, string PATH)
        {
            string[] Derivation = PATH.Split('/');

            byte[] masterPrivateKey = Private;
            byte[] masterChainCode = Chain;
            var DER = new List<long>();
            //Console.WriteLine(Derivation[0]);
            foreach (string sub in Derivation)
            {
                if (sub.Contains("'"))
                {
                    //Console.WriteLine(sub);
                    var ser = Int32.Parse(sub[..^1]);
                    DER.Add(0x80000000 + ser);
                }
                else
                {
                    //Console.WriteLine(sub);
                    DER.Add(Int32.Parse(sub));
                }
            }
            string keys;
            byte[] key, D;

            byte[] PrivateKey = new byte[32]; //private key
            byte[] ChainCode = new byte[32];
            //for (var n = 0; n < DER.Count; n++)
            foreach (var n in DER)
            //var n;
            //Parallel.ForEach(DER, n)
            {

                var k = masterChainCode;
                string d;
                if ((n & 0x80000000) != 0)
                {
                    byte[] b = ASCIIEncoding.ASCII.GetBytes("\x00");
                    keys = BytesToHexString(b) + BytesToHexString(masterPrivateKey);
                    key = StringToByteArray(keys);
                }
                else
                {
                    int range = BytesToHexString(masterPrivateKey).Length;
                    //Console.WriteLine("Проверка ключа перед ошибкой " + BytesToHexString(masterPrivateKey) + " Длинна: " + range);

                    if (range != 64)
                    {
                        while (range != 64)
                        {
                            //Console.WriteLine("DAA!");
                            string PVK64 = BytesToHexString(masterPrivateKey);
                            //Console.WriteLine(PVK64);
                            masterPrivateKey = StringToByteArray("00" + PVK64);
                            //Console.WriteLine("Проверка ключа ппосле добвки нулей " + BytesToHexString(masterPrivateKey));
                            range++;
                            range++;
                        }
                    }
                    key = Cryptography.ECDSA.Secp256K1Manager.GetPublicKey(masterPrivateKey, true);
                }
                d = BytesToHexString(key) + n.ToString("X8");
                D = StringToByteArray(d);
                while (true)
                {
                    var HMAC = new HMACSHA512(k);
                    var h = HMAC.ComputeHash(D);

                    Buffer.BlockCopy(h, 0, PrivateKey, 0, 32);
                    Buffer.BlockCopy(h, 32, ChainCode, 0, 32);
                    BigInteger a = BigInteger.Parse("0" + BytesToHexString(PrivateKey), System.Globalization.NumberStyles.AllowHexSpecifier);
                    BigInteger b = BigInteger.Parse("0" + BytesToHexString(masterPrivateKey), System.Globalization.NumberStyles.AllowHexSpecifier);
                    BigInteger key1 = (a + b) % order;
                    if ((key1.ToString("X64").Length > 64))
                    {
                        var key_string = key1.ToString("X64")[^64..];
                        key1 = BigInteger.Parse(key_string, System.Globalization.NumberStyles.AllowHexSpecifier);
                    }
                    if ((a < order) && (key1 != 0))
                    {
                        key = key1.ToByteArray(false, true);
                        masterPrivateKey = key;
                        masterChainCode = ChainCode;
                        break;
                    }
                    byte[] b2 = ASCIIEncoding.ASCII.GetBytes("\x01");
                    var dd = BytesToHexString(b2) + BytesToHexString(ChainCode) + n.ToString("X8");
                    D = StringToByteArray(dd);

                }
            }
            return masterPrivateKey;
        }

        private static string BytesToBinary(byte[] hash)
        {
            return string.Join("", hash.Select(h => LeftPad(Convert.ToString(h, 2), "0", 8)));
        }

        private static string LeftPad(string str, string padString, int length)
        {
            while (str.Length < length)
            {
                str = padString + str;
            }

            return str;
        }

        private static string DeriveChecksumBits(byte[] checksum)
        {
            var ent = checksum.Length * 8;
            var cs = ent / 32;

            var sha256Provider = new SHA256CryptoServiceProvider();
            var hash = sha256Provider.ComputeHash(checksum);
            var result = BytesToBinary(hash);
            return result.Substring(0, cs);
        }

        private static string[] GetWordList(WordListLanguage wordList)
        {
            var wordLists = new Dictionary<string, string>
            {
                {WordListLanguage.ChineseSimplified.ToString(), "chinese_simplified"},
                {WordListLanguage.ChineseTraditional.ToString(), "chinese_traditional"},
                {WordListLanguage.English.ToString(), "english"},
                {WordListLanguage.French.ToString(), "french"},
                {WordListLanguage.Italian.ToString(), "italian"},
                {WordListLanguage.Japanese.ToString(), "japanese"},
                {WordListLanguage.Korean.ToString(), "korean"},
                {WordListLanguage.Spanish.ToString(), "spanish"}
            };

            var wordListFile = wordLists[wordList.ToString()];

            var assembly = Assembly.GetAssembly(typeof(WordListLanguage));
            var wordListFileStream = assembly.GetManifestResourceStream($"{typeof(WordListLanguage).Namespace}.Words.{wordListFile}.txt");

            var words = new List<string>();
            using (var reader = new StreamReader(wordListFileStream ?? throw new InvalidOperationException($"could not load word list for {wordList}")))
            {
                while (reader.Peek() >= 0)
                {
                    words.Add(reader.ReadLine());
                }
            }

            var wordListResults = words.ToArray();
            return wordListResults;
        }

        public static string EntropyToMnemonic(byte[] entropyBytes, string[] wordList, WordListLanguage wordListType)
        {
            CheckValidEntropy(entropyBytes);

            var entropyBits = BytesToBinary(entropyBytes);
            var checksumBits = DeriveChecksumBits(entropyBytes);

            var bits = entropyBits + checksumBits;

            var chunks = Regex.Matches(bits, "(.{1,11})")
                .OfType<Match>()
                .Select(m => m.Groups[0].Value)
                .ToArray();

            var words = chunks.Select(binary =>
            {
                var index = Convert.ToInt32(binary, 2);
                return wordList[index];
            });

            var joinedText = string.Join((wordListType == WordListLanguage.Japanese ? "\u3000" : " "), words);

            return joinedText;
        }

        private static void CheckValidEntropy(byte[] entropyBytes)
        {
            if (entropyBytes == null)
                throw new ArgumentNullException(nameof(entropyBytes));

            if (entropyBytes.Length < 4)
                throw new ArgumentException(InvalidEntropy);

            if (entropyBytes.Length > 32)
                throw new ArgumentException(InvalidEntropy);

            if (entropyBytes.Length % 4 != 0)
                throw new ArgumentException(InvalidEntropy);
        }

        public static byte[] GenerateMnemonicBytes(int strength)
        {
            if (strength % 32 != 0)
                throw new NotSupportedException(InvalidEntropy);

            var rngCryptoServiceProvider = new RNGCryptoServiceProvider();

            var buffer = new byte[strength / 8];
            rngCryptoServiceProvider.GetBytes(buffer);

            return buffer;
        }

        public static string GetHash160(byte[] public_key)
        {

            var SHA256 = Sha256Manager.GetHash(public_key);
            var hash160 = BytesToHexString(Ripemd160Manager.GetHash(SHA256));

            return hash160;

        }

        public static string GetAddress(byte[] public_key)
        {

            var SHA256 = Sha256Manager.GetHash(public_key);
            var hash160 = Ripemd160Manager.GetHash(SHA256);
            //Console.WriteLine("hash160: " + BytesToHexString(hash160));
            var add_prefix = StringToByteArray(prefix_legacy + BytesToHexString(hash160));
            SHA256 = Sha256Manager.GetHash(add_prefix);
            SHA256 = Sha256Manager.GetHash(SHA256);
            var checksum = StringToByteArray(BytesToHexString(add_prefix) + BytesToHexString(SHA256)[..8]);
            var address = Base58.Encode(checksum);
            //Console.WriteLine("address: " + comressed_address);

            return address;

        }
        public static string GetEthAddress(byte[] public_key)
        {

            var pub = StringToByteArray(BytesToHexString(public_key)[^128..]);
            var keccak = Keccak256.ComputeHash(pub);
            var address = BytesToHexString(keccak)[^40..];
            return address;
        }
        public static string GetSegWit_base58(byte[] public_key)
        {
            var SHA256 = Sha256Manager.GetHash(public_key);
            var hash160 = Ripemd160Manager.GetHash(SHA256);
            var add_prefix = StringToByteArray("0014" + BytesToHexString(hash160));
            SHA256 = Sha256Manager.GetHash(add_prefix);
            hash160 = Ripemd160Manager.GetHash(SHA256);
            add_prefix = StringToByteArray(prefix_segwit + BytesToHexString(hash160));
            SHA256 = Sha256Manager.GetHash(add_prefix);
            SHA256 = Sha256Manager.GetHash(SHA256);
            var checksum = StringToByteArray(BytesToHexString(add_prefix) + BytesToHexString(SHA256)[..8]);
            var address = Base58.Encode(checksum);

            return address;

        }
        // <summary>
        /// Turns a byte array into a Hex encoded string
        /// </summary>
        /// <param name="bytes">The bytes to encode to hex</param>
        /// <returns>The hex encoded representation of the bytes</returns>
        public static string BytesToHexString(byte[] bytes, bool upperCase = false)
        {
            if (upperCase)
            {
                return string.Concat(bytes.Select(byteb => byteb.ToString("X2")).ToArray());
            }
            else
            {
                return string.Concat(bytes.Select(byteb => byteb.ToString("x2")).ToArray());
            }
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
