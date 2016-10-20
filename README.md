# Hierarchy
## With MVC and Entity Framework

The goal is to facilitate operations in hierarchical tables using functions available for hierarchyid data type.  
There are two challenges:

*   Simplify use through set of simple commands.
*   Using Entity Framework without changes, enable hierarchy operations in other databases, in addition to SQL Server.

### How?

We will use only five basic operations to maintain the hierarchy:

*   Adding a new element (as last child of a given parent).
*   Promotion. To the same level of his parent.
*   Demotion. Becomes the last child of the previous sibling.
*   Moving up approaching the parent.
*   Move down away from the parent.

Through these operations it is possible to position a record anywhere in the hierarchy.

[Learn more »](https://www.youtube.com/embed/zuqZCAz7P88)

### Details

*   We will not use the hierarchyid field. In its place a varbinary field (892). This will allow the Entity Framework to map the field.
*   In hierarchy services functions we can convert the byte array data to SqlHierarchyid data type.
*   So the application can use the basic functions of the hierarchy handling.
*   This is even possible for other databases in addition to MS SQL Server.
*   Just use to the field that stores the hierarchyid data, a data type that is mapped to byte array.
*   For MySql, for example, varbinary (and blob) worked in this application.

See the source code!

###Sample Projects on GitHub

*   HidMy – Accessing MySql using Hierarchy.Universal namespace.
*   HidMS – Accessing Sql Server using Hierarchy.SqlServer namespace.
*   Both project will load nuget package EntityFramework.Hierarchy.
 
