Bitcoins (seehttp://en.wikipedia.org/wiki/Bitcoin) are the most popular crypto-currency in common use. At their heart, bitcoins use the hardness of cryptographic hashing (for a reference seehttp://en.wikipedia.org/wiki/Cryptographichashfunction)to ensure a limited “supply” of coins.  In particular, the key component in a bit-coin is an input that, when “hashed” produces an output smaller than a target value.  In practice, the comparison values have leading  0’s, thus the bitcoin is required to have a given number of leading 0’s (to ensure 3 leading 0’s, you look for hashes smaller than0x001000... or smaller or equal to 0x000ff....the hash used is SHA-256. The goal of this project is to use F# and the actor model to build a good solution to this problem that runs well on multi-core machines.

How to run the code: 
The input line should be the number of zeros required at the beginning of the bitcoin: 
For instance: 
> 1

The corresponding output for this should be: 
> 0d402337f95d018438aad6c7dd75ad6e9239d6060444a7a6b26299b261aa9a8b

This value is the bitcoin that was mined. 
