namespace OpcUaTestServer.Utilities
{
    using Opc.Ua;

    internal class NodeManagerUtilities(ushort namespaceIndex)
    {

        public void AddRobotToCell(FolderState cellFolder, string robotName)
        {
            FolderState robotGroupFolder = CreateFolder(cellFolder, robotName, robotName);

            CreateVariable(robotGroupFolder, "MANUALOFFLINE", "MANUALOFFLINE", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(robotGroupFolder, "PICKPROGRAM", "PICKPROGRAM", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(robotGroupFolder, "PLACEPROGRAM", "PLACEPROGRAM", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(robotGroupFolder, "PROCESSPROGRAM", "PROCESSPROGRAM", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(robotGroupFolder, "ROBOT_CYCLE_START", "ROBOT_CYCLE_START", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(robotGroupFolder, "ROBOT_MOTOR_ON", "ROBOT_MOTOR_ON", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(robotGroupFolder, "ROBOT_MOTORPOWER_IS_ON", "ROBOT_MOTORPOWER_IS_ON", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(robotGroupFolder, "ROBOT_READY", "ROBOT_READY", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(robotGroupFolder, "ROBOT_STATUS", "ROBOT_STATUS", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(robotGroupFolder, "SAFETOPLACE", "SAFETOPLACE", DataTypeIds.UInt16, ValueRanks.Scalar);
        }

        public void AddConveyorToCell(FolderState cellFolder, string conveyorName)
        {
            FolderState conveyorGroupFolder = CreateFolder(cellFolder, conveyorName, conveyorName);
            CreateVariable(conveyorGroupFolder, "ENABLED", "ENABLED", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(conveyorGroupFolder, "IN_POSITION", "IN_POSITION", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(conveyorGroupFolder, "POSITION_NUMBER", "POSITION_NUMBER", DataTypeIds.UInt16, ValueRanks.Scalar, (ushort)1);
            CreateVariable(conveyorGroupFolder, "STARTINDEX", "STARTINDEX", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(conveyorGroupFolder, "STATUS", "STATUS", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(conveyorGroupFolder, "START_ONLINE_BEACON", "START_ONLINE_BEACON", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(conveyorGroupFolder, "FINISH_ONLINE_BEACON", "FINISH_ONLINE_BEACON", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(conveyorGroupFolder, "SET_CONVEYOR_POSITION", "SET_CONVEYOR_POSITION", DataTypeIds.UInt16, ValueRanks.Scalar, (ushort)0);
            CreateVariable(conveyorGroupFolder, "CONVEYOR_UNLOAD_PART_CLEAR", "CONVEYOR_UNLOAD_PART_CLEAR", DataTypeIds.UInt16, ValueRanks.Scalar, (ushort)0);
            CreateVariable(conveyorGroupFolder, "MANUAL_START", "MANUAL_START", DataTypeIds.UInt16, ValueRanks.Scalar, (ushort)0);
            CreateVariable(conveyorGroupFolder, "MANUAL_STOP", "MANUAL_STOP", DataTypeIds.UInt16, ValueRanks.Scalar, (ushort)0);
        }

        public void AddSystemTagsToCell(FolderState cellFolder, bool allManualModeTags = true)
        {
            FolderState systemGroupFolder = CreateFolder(cellFolder, "System", "System");

            CreateVariable(systemGroupFolder, "IN_AUTO", "IN_AUTO", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(systemGroupFolder, "RUN", "RUN", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(systemGroupFolder, "SAFE_TO_RUN", "SAFE_TO_RUN", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(systemGroupFolder, "SAFE_TO_STOP", "SAFE_TO_STOP", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(systemGroupFolder, "MANUAL_INDEX_TO_POSITION", "MANUAL_INDEX_TO_POSITION", DataTypeIds.UInt16, ValueRanks.Scalar, (ushort)0);
            CreateVariable(systemGroupFolder, "SUPERVISOR_KEY_SWITCH", "SUPERVISOR_KEY_SWITCH", DataTypeIds.UInt16, ValueRanks.Scalar, (ushort)0);
            CreateVariable(systemGroupFolder, "TEMPERATURE", "TEMPERATURE", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(systemGroupFolder, "RELATIVE_HUMIDITY", "RELATIVE_HUMIDITY", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(systemGroupFolder, "SEND_TO_HOME", "SEND_TO_HOME", DataTypeIds.UInt16, ValueRanks.Scalar);

            // manual cell mode tags
            CreateVariable(systemGroupFolder, "SANDER1_ON", "SANDER1_ON", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(systemGroupFolder, "SANDER2_ON", "SANDER2_ON", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(systemGroupFolder, "SLURRYTANK1_ON", "SLURRYTANK1_ON", DataTypeIds.UInt16, ValueRanks.Scalar);
            CreateVariable(systemGroupFolder, "SLURRYTANK2_ON", "SLURRYTANK2_ON", DataTypeIds.UInt16, ValueRanks.Scalar);

            if (allManualModeTags)
            {
                CreateVariable(systemGroupFolder, "SLURRYTANK3_ON", "SLURRYTANK3_ON", DataTypeIds.UInt16, ValueRanks.Scalar);
                CreateVariable(systemGroupFolder, "SLURRYTANK1_LID_OPEN", "SLURRYTANK1_LID_OPEN", DataTypeIds.UInt16, ValueRanks.Scalar);
                CreateVariable(systemGroupFolder, "SLURRYTANK2_LID_OPEN", "SLURRYTANK2_LID_OPEN", DataTypeIds.UInt16, ValueRanks.Scalar);
                CreateVariable(systemGroupFolder, "WASHTANK_LID_OPEN", "WASHTANK_LID_OPEN", DataTypeIds.UInt16, ValueRanks.Scalar);
                CreateVariable(systemGroupFolder, "RINSETANK_LID_OPEN", "RINSETANK_LID_OPEN", DataTypeIds.UInt16, ValueRanks.Scalar);
                CreateVariable(systemGroupFolder, "SANDER3_ON", "SANDER3_ON", DataTypeIds.UInt16, ValueRanks.Scalar);
            }
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        internal BaseDataVariableState CreateVariable(NodeState? parent, string path, string name, NodeId dataType, int valueRank, object? initialValue = null)
        {
            if (parent != null)
            {
                path = $"{parent.NodeId.Identifier}.{path}";
            }

            BaseDataVariableState variable = new(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                NodeId = new NodeId(path, namespaceIndex),
                BrowseName = new QualifiedName(name, namespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                DataType = dataType,
                ValueRank = valueRank,
                AccessLevel = AccessLevels.CurrentReadOrWrite,
                UserAccessLevel = AccessLevels.CurrentReadOrWrite,
                Historizing = false,
                MinimumSamplingInterval = 10,
                Description = "",
                Value = initialValue ?? (ushort)0
            };

            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            variable.ArrayDimensions = valueRank switch
            {
                ValueRanks.OneDimension => new ReadOnlyList<uint>(new List<uint> { 0 }),
                ValueRanks.TwoDimensions => new ReadOnlyList<uint>(new List<uint> { 0, 0 }),
                _ => variable.ArrayDimensions
            };

            parent?.AddChild(variable);

            return variable;
        }

        internal FolderState CreateFolder(NodeState? parent, string path, string name)
        {
            if (parent != null)
            {
                path = $"{parent.NodeId.Identifier}.{path}";
            }

            FolderState folder = new(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                NodeId = new NodeId(path, namespaceIndex),
                BrowseName = new QualifiedName(name, namespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                EventNotifier = EventNotifiers.None
            };

            parent?.AddChild(folder);

            return folder;
        }
    }
}
