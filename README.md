# SQLiteDBAccess

This is a lightweight sqlite access library.
It allows you to easily create and access multiple different sqlite db files.

simply add/create a database via `SQLiteDBAccess.Instance()` This will check if there already is an instance for that file and ether create the file and return a new instance or get the already existing instance. 

You can also pass a bool as 3rd attribute (default `true`) it determines if the file should be managed automatically or manually. 
If set to `true` the file will be opened and closed on every call. 
If that is to expensive for you you can pass `false` and open/close the file manually via `OpenDBFile()` and `CloseDBFile()`.

### **Attention**
**If a method in SQLiteDBAccess is used that returns an SQLDataReader the connection will not be closed even if file management is enabled. 
Meaning you will have to manually free the file with `CloseDBFile(yourReader)`. 
`yourReader` being the SQLiteDataReader that you have used.
This is necessary because the connection needs to be active to use the reader.**

You can also run any sqlite command via `CustiomCommand` or `CustiomCommandWithReturn` if you want to return value(s).

## examples:

### Create/Add Database
```csharp
var dbAccess = SQLiteDBAccess.SQLiteDBAccess.Instance("mydb", "/home/teddy"); // on windows path would be "C:/Users/teddy"
```

Note: 
Depending on if your file system is case sensitive creating a Database with the name `mydb` and `Mydb` will ether be the same (on non-case sensitive) or different (on case sensitive) instances and files.

### Create Table
````csharp
dbAccess.CreateTable("MyTable", "Id INTEGER PRIMARY KEY, MyText TEXT, MyNumber INTEGER");
````

### Insert Into Table
````csharp
var myNewText = "'hello world'";
var myNewNumber = 1337;
dbAccess.Insert("MyTable", "MyText, MyNumber", $"{myNewText}, {myNewNumber}");
````

Note:
You will need to add '' around items which contain spaces e.g. `'hello world'`.
### Read Single Row From Table By Attribute
````csharp
var reader = dbAccess.GetByAttribute("MyTable", "Id", "1");
if (reader.Read())
{
    Console.WriteLine($"{reader.GetName(0)}: {reader.GetInt64(0)}\n{reader.GetName(1)}: {reader.GetString(1)}\n{reader.GetName(2)}: {reader.GetInt64(2)}");
} 
dbAccess.CloseDBFile(reader);
````

Note: 
GetByAttribute() can return multiple rows. In this example a primary key is used and result.Read() is only executed once. 
Any one of those factors will result in a single row being returned. 

### Read All Rows From Table
````csharp
var reader = dbAccess.GetAll("MyTable");
while (reader.Read())
{
    Console.WriteLine($"{reader.GetName(0)}: {reader.GetInt64(0)}\n{reader.GetName(1)}: {reader.GetString(1)}\n{reader.GetName(2)}: {reader.GetInt64(2)}");
} 
dbAccess.CloseDBFile(reader);
````

Note:
- This Nuget Package is geared more towards smaller projects and will result in a multitude of issues if used in a bigger project. 
- This Nuget Package should not and was not created to be used with unsanitized user input.