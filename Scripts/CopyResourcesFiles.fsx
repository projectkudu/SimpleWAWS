open System
open System.IO

let source = fsi.CommandLineArgs.[1].Trim ()
let dest = fsi.CommandLineArgs.[2].Trim ()
Directory.CreateDirectory dest |> ignore
let fullNameLanguages = [|"en-us"; "pt-br"; "zh-tw"; "zh-cn"; |]

let getCulture (fullName: string) =
    if fullNameLanguages |> Array.exists ((=) fullName) then fullName
    else (fullName.Split '-').[0]

Directory.GetDirectories source
|> Array.iter (fun dir ->
    let culture = getCulture (Path.GetFileName dir)
    let files = Directory.GetFiles (dir, "*", SearchOption.AllDirectories)
    for file in files do
        let extension = Path.GetExtension file
        let fileName = Path.GetFileNameWithoutExtension file
        let outputName = fileName + "." + culture + extension
        File.Copy (file, Path.Combine (dest, outputName)) )
