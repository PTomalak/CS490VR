use std::collections::HashMap;
use std::path::Path;
use std::process::Command;

use bimap::BiHashMap;
use cgmath::Vector3;
use kdtree::KdTree;
use petgraph::{Graph, Undirected};
use petgraph::dot::Dot;
use petgraph::graph::NodeIndex;

use crate::block::{Block, GridCoord, is_adjacent, MANHATTAN_ADJACENT_CUBE, manhattan_distance_ref, VOXEL_CIRCUIT, VoxelID};

// use serde::{Deserialize, Serialize};

pub type InstanceID = u32;
pub type WireDummy = u32;
pub type NodeIDType = u32;
pub type NodeID = NodeIndex<NodeIDType>;

pub const BLANK: WireDummy = 0;

/*
pub type Resistance = u32;

#[derive(Clone, Debug, Default, Serialize, Deserialize)]
pub struct VoxelConnection(VoxelConnectionID, Resistance);

impl Display for VoxelConnection
{
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        write!(f, "{:?}", self)
    }
}
 */

#[derive(Clone, Debug)]
pub struct Scene
{
    blocks: HashMap<InstanceID, Block>,
    voxels: BiHashMap<(InstanceID, VoxelID), NodeID>,

    circuit: Graph<(InstanceID, VoxelID), WireDummy, Undirected, NodeIDType>,
    space: KdTree<f32, (InstanceID, VoxelID), GridCoord>,
}

impl Default for Scene
{
    fn default() -> Self {
        Self {
            blocks: Default::default(),
            voxels: Default::default(),
            circuit: Default::default(),
            space: KdTree::new(3usize),
        }
    }
}

impl Scene
{
    pub fn new() -> Self {
        Self::default()
    }

    pub fn save_debug_circuit(&self, path: &Path) {
        let d = Dot::with_config(&self.circuit, &[]);
        std::fs::write(&path, format!("{:?}", d)).unwrap();
        Command::new("dot")
            .args(["-Tps", path.to_str().unwrap(), "-o", &format!("{}.pdf", path.to_str().unwrap())])
            .spawn()
            .unwrap();
    }

    /*
    /// Adds the given block ID's block to the circuit representation
    fn add_block_circuit(&mut self, block_id: InstanceID, connections: HashMap<VoxelTerminalID, (InstanceID, VoxelTerminalID)>) {
        let block = self.blocks.get(&block_id).unwrap();

        for (connection_id, target) in connections {
            self.circuit.add_edge(idx, target.into(), connection_id);
        }
    }
     */

    /// Adds the given block at the given location and updates the internal circuit
    /// Returns the block's ID or `None` if a block overlaps an existing block
    pub fn add_block(&mut self, block: Block, location: GridCoord) -> Option<InstanceID> {
        let id = self.blocks.len() as u32;

        self.blocks.insert(id, block.clone()).unwrap();

        // Check if block overlaps existing block

        for (structure_id, _) in block.get_structure() {
            if self.space.add(location, (id, structure_id)).ok().is_some() {
                return None;
            }
        }

        // Add block connections

        // Filter out non-circuit voxels and add to circuit
        let terminal_node_ids = block.get_structure()
            .iter()
            .filter(|e| e.0.starts_with(VOXEL_CIRCUIT))
            .map(|(tid, gc)| {
                let d = (id, tid.clone());
                let result = (tid.clone(), *gc, self.circuit.add_node(d.clone()));
                self.voxels.insert(d, result.2);
                return result;
            })
            .collect::<Vec<(VoxelID, GridCoord, NodeID)>>();

        // Iterate over the circuit voxels (terminals) that make up this block
        for (_, t, terminal_node_id) in terminal_node_ids {
            // Get all potential voxel terminals
            let neighbors = self.space.nearest(GridCoord::from(Vector3::<f32>::from(t.0) + Vector3::<f32>::from(location.0)).as_ref(),
                                               MANHATTAN_ADJACENT_CUBE,
                                               &manhattan_distance_ref).ok()?;

            // Iterate over neighbors
            for (neighbor_terminal_distance, neighbor_weight) in neighbors {
                // If neighbor is adjacent to this new block's current terminal
                if neighbor_terminal_distance == 1.0 {
                    self.circuit.update_edge(terminal_node_id, *self.voxels.get_by_left(&neighbor_weight).unwrap(), BLANK);
                }
            }
        }

        Some(id)
    }

    /// Removes the block with the given ID
    pub fn _remove_block(&mut self, _id: InstanceID) -> Option<()> {
        todo!();
    }

    /// Add a wire that follows the given path
    /// Returns the wire block IDs if the path is valid, `None` if not
    pub fn add_wire(&mut self, path: Vec<GridCoord>) -> Option<Vec<InstanceID>> {
        let mut last = path.first().copied()?;

        // Verify path is continuous
        for step in path.iter().skip(1).copied() {
            if !is_adjacent(last, step) {
                return None;
            }
            last = step;
        }

        Some(path.iter().copied().map(|e| self.add_block(Block::Wire(Default::default()), e).unwrap()).collect())
    }
}

#[test]
pub fn wire_test()
{
    let mut scene = Scene::new();

    assert!(scene.add_wire((0..100).map(|e| GridCoord::new(0, 0, e)).collect()).is_some());
    assert!(scene.add_wire((0..100).map(|e| GridCoord::new(0, 0, 2 * e)).collect()).is_none());

    scene.save_debug_circuit(&Path::new("./result.dot"));
}