Relational Components – Basic

This sample demonstrates the built-in Relational Component Attributes without any DI container. Attach the script to a GameObject hierarchy and the fields are auto-assigned at runtime.

How to use

- Open any scene and create a simple hierarchy with a parent, a child, and a sibling next to the consumer object.
- Add `RelationalBasicConsumer` to the child object.
- Enter Play Mode and check the Console for the assigned references.

What it shows

- `[ParentComponent]` finds components on ancestors (configurable depth).
- `[SiblingComponent]` finds components on the same GameObject.
- `[ChildComponent]` finds components on children (recursively).
- `this.AssignRelationalComponents()` hydrates all relational fields in `Awake()`/`OnEnable()`.

Notes

- You can adjust filters on attributes: `Optional`, `IncludeInactive`, `TagFilter`, `NameFilter`, and `MaxCount` for collections.
- For DI integrations, see the separate VContainer/Zenject samples.

