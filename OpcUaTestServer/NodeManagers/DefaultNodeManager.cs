namespace OpcUaTestServer.NodeManagers
{
    using Opc.Ua.Server;
    using Opc.Ua;
    using Server;
    using Utilities;

    internal class DefaultNodeManager : CustomNodeManager2
    {
        private readonly NodeManagerUtilities nodeManagerUtilities;
        private readonly string rootFolder;

        public DefaultNodeManager(IServerInternal server, ApplicationConfiguration configuration, string rootFolder)
            : base(server, configuration, ServerNamespaces.TestServer)
        {
            SystemContext.NodeIdFactory = this;
            nodeManagerUtilities = new NodeManagerUtilities(NamespaceIndex);
            this.rootFolder = rootFolder ?? string.Empty;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {

            }
        }

        public override NodeId New(ISystemContext context, NodeState node)
        {
            return node.NodeId;
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>?> externalReferences)
        {
            lock (Lock)
            {
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out IList<IReference>? references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                FolderState root = nodeManagerUtilities.CreateFolder(null, rootFolder, rootFolder);
                root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                references?.Add(new NodeStateReference(ReferenceTypes.Organizes, false, root.NodeId));
                root.EventNotifier = EventNotifiers.SubscribeToEvents;
                AddRootNotifier(root);

                try
                {
                    #region CellOne

                    FolderState cell1Folder = nodeManagerUtilities.CreateFolder(root, "Cell1", "Cell1");
                    nodeManagerUtilities.AddConveyorToCell(cell1Folder, "Conveyor1");
                    nodeManagerUtilities.AddConveyorToCell(cell1Folder, "Conveyor2");
                    nodeManagerUtilities.AddRobotToCell(cell1Folder, "Robot1");
                    nodeManagerUtilities.AddSystemTagsToCell(cell1Folder);

                    #endregion

                    #region CellTwo

                    FolderState cell2Folder = nodeManagerUtilities.CreateFolder(root, "Cell2", "Cell2");
                    nodeManagerUtilities.AddConveyorToCell(cell2Folder, "Conveyor1");
                    nodeManagerUtilities.AddConveyorToCell(cell2Folder, "Conveyor2");
                    nodeManagerUtilities.AddRobotToCell(cell2Folder, "Robot1");
                    nodeManagerUtilities.AddSystemTagsToCell(cell2Folder, false);

                    #endregion

                }
                catch (Exception e)
                {
                    Utils.LogError(e, "Error creating the ReferenceNodeManager address space.");
                }

                AddPredefinedNode(SystemContext, root);
            }
        }
    }
}
