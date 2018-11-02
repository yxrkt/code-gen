namespace Parser

module Utilities =

    let nextPowerOfTwo (n: int) =
        let n = n - 1
        let n = n ||| (n >>> 1)
        let n = n ||| (n >>> 2)
        let n = n ||| (n >>> 4)
        let n = n ||| (n >>> 8)
        let n = n ||| (n >>> 16)
        n + 1

    let countBitsSet (n: int) =
        let n = n - ((n >>> 1) &&& 0x55555555)
        let n = (n &&& 0x33333333) + ((n >>> 2) &&& 0x33333333)
        (((n + (n >>> 4)) &&& 0x0F0F0F0F) * 0x01010101) >>> 24

    let getBitsNeeded (n: int) =
        if n <= 2 then
            1
        else
            (n |> nextPowerOfTwo) - 1 |> countBitsSet