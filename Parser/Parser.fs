namespace Parser

module Parser =

    open FSharp.Data
    open Utilities

    type Definitions = JsonProvider<"schema.json">

    let definitions = Definitions.GetSample()

    type Type = {
        Name: string
        Size: int
    }

    type TypeInfoPart =
        | Field of Name: string * Type: Type
        | UnionHeader of UnionID: int * Name: string option * Size: int
        | UnionField of UnionID: int * Name: string * Type: Type

    let builtInTypes = List.map (fun (name, size) -> (name, { Name = name; Size = size })) [
        ("bool", 1)
        ("s8", 8)
        ("u8", 8)
        ("s16", 16)
        ("u16", 16)
        ("s32", 32)
        ("u32", 32)
        ("s64", 64)
        ("u64", 64)
        ("f32", 32)
        ("f32", 32)
        ("f64", 64)
        ("f64", 64)
    ]

    let knownTypes = ref (builtInTypes |> Map.ofList)

    let getUnionParts (id: int ref) (cases: Definitions.Casis array) =
        let header = TypeInfoPart.UnionHeader (!id, None, getBitsNeeded cases.Length)
        do id := !id + 1
        let partLists: TypeInfoPart list [] =
            cases |> Array.map (fun case -> case.Properties) |> Array.map (fun properties ->
                properties |> Array.map (fun property ->
                    if property.Type <> null then
                        property.
                )
            )

        let body = partLists |> Array.fold (fun total i -> total @ i) []

        header::body

    let public result =
        definitions.Types |> Array.map (fun t ->
            let nextUnionID = ref 0
            let partLists =
                if not (Array.isEmpty t.Properties) then
                    t.Properties |> Array.map (fun p ->
                        if p.Type.IsSome then
                            [TypeInfoPart.Field (p.Name.Value, (knownTypes.Value.TryFind p.Type.Value).Value)]
                        else
                            p.Cases |> getUnionParts nextUnionID
                    )
                else
                    failwith "todo"

            let parts = partLists |> Array.fold (fun total i -> total @ i) []
            parts
        )