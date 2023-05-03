use std::collections::HashMap;
use std::fmt::{Display, Formatter};

use cgmath::Zero;
use serde::{Deserialize, Serialize};

use crate::grid::Coord;

pub type VoxelID = String;
pub type PowerState = bool;
pub type _Color = (u8, u8, u8, u8);

const VOXEL_CIRCUIT: &str = "!";

pub fn circuit_voxel(name: &str) -> String
{
    format!("{}{}", VOXEL_CIRCUIT, name)
}

pub fn is_circuit_voxel(name: &str) -> bool
{
    name.starts_with(VOXEL_CIRCUIT)
}

#[derive(Clone, Copy, Debug, PartialEq, Serialize, Deserialize)]
pub enum Orient
{
    /// Facing +Z
    FORWARD,
    /// Facing +X
    RIGHT,
    /// Facing -X
    LEFT,
    /// Facing -Z
    BACKWARD,
    /// Facing +Y
    UPWARD,
    /// Facing -Y
    DOWNWARD,
}

impl Default for Orient
{
    fn default() -> Self {
        Self::FORWARD
    }
}

#[derive(Clone, Debug, Default, PartialEq, Serialize, Deserialize)]
pub struct VoxelPowered
{
    pub powered: bool,
}

#[derive(Clone, Debug, Default, PartialEq, Serialize, Deserialize)]
pub struct VoxelMemory
{
    pub stored: bool,
    pub powered: bool,
}

#[derive(Clone, Debug, Default, PartialEq, Serialize, Deserialize)]
pub struct VoxelPulseLatch
{
    pub pulse_ticks: u32,
    pub pulse_battery: u32,
    pub powered: bool,
}

#[derive(Clone, Debug, Default, PartialEq, Serialize, Deserialize)]
pub struct VoxelPixel
{
    pub powered: bool,
}

#[derive(Clone, Debug, Default, PartialEq, Serialize, Deserialize)]
pub struct VoxelBlock
{}

#[derive(Clone, Debug, Default, PartialEq, Serialize, Deserialize)]
pub struct VoxelClock
{
    pub rate: u32,
    pub start_tick: u32,
    pub powered: bool,
}

#[derive(Clone, Debug, Default, PartialEq, Serialize, Deserialize)]
pub struct VoxelPulse
{
    pub start_tick: u32,
    pub pulse_ticks: u32,
    pub powered: bool,
}

#[derive(Clone, Debug, PartialEq, Serialize, Deserialize)]
#[serde(tag = "block", content = "data")]
pub enum Block
{
    #[serde(rename = "air")]
    Air,

    #[serde(rename = "wire")]
    Wire(VoxelPowered),
    #[serde(rename = "block")]
    Block(VoxelBlock),
    #[serde(rename = "toggle")]
    Toggle(VoxelPowered),
    #[serde(rename = "pixel")]
    Pixel(VoxelPixel),

    #[serde(rename = "and_gate")]
    ANDGate(VoxelPowered),
    #[serde(rename = "or_gate")]
    ORGate(VoxelPowered),
    #[serde(rename = "xor_gate")]
    XORGate(VoxelPowered),
    #[serde(rename = "nand_gate")]
    NANDGate(VoxelPowered),
    #[serde(rename = "nor_gate")]
    NORGate(VoxelPowered),
    #[serde(rename = "xnor_gate")]
    XNORGate(VoxelPowered),
    #[serde(rename = "not_gate")]
    NOTGate(VoxelPowered),
    #[serde(rename = "diode")]
    Diode(VoxelPowered),

    #[serde(rename = "clock")]
    Clock(VoxelClock),

    #[serde(rename = "pulse")]
    Pulse(VoxelPulse),

    #[serde(rename = "toggle_latch")]
    ToggleLatch(VoxelMemory),
    #[serde(rename = "pulse_latch")]
    PulseLatch(VoxelPulseLatch),
    #[serde(rename = "memory_latch")]
    MemoryLatch(VoxelMemory),
}

impl Display for Block
{
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        write!(f, "{:?}", self)
    }
}

impl Default for Block
{
    fn default() -> Self {
        Self::Air
    }
}

impl Block
{
    /// Get this block's power state and independence by voxel (returns value only if applicable)
    ///
    /// Internal: MUST match voxel definitions in `get_structure`
    pub fn get_circuit_voxel_power(&self) -> HashMap<VoxelID, Option<PowerState>> {
        match self {
            Block::Air => Default::default(),
            Block::Wire(data) => [(circuit_voxel("wire"), Some(data.powered))].into(),
            Block::Block(_) => Default::default(),
            Block::Toggle(data) => [(circuit_voxel("toggle"), Some(data.powered))].into(),
            Block::Pixel(_) => [(circuit_voxel("pixel"), None)].into(),
            Block::ANDGate(data) => [
                (circuit_voxel("in_a"), None),
                (circuit_voxel("in_b"), None),
                (circuit_voxel("out"), Some(data.powered)),
            ].into(),
            Block::ORGate(data) => [
                (circuit_voxel("in_a"), None),
                (circuit_voxel("in_b"), None),
                (circuit_voxel("out"), Some(data.powered)),
            ].into(),
            Block::XORGate(data) => [
                (circuit_voxel("in_a"), None),
                (circuit_voxel("in_b"), None),
                (circuit_voxel("out"), Some(data.powered)),
            ].into(),
            Block::NANDGate(data) => [
                (circuit_voxel("in_a"), None),
                (circuit_voxel("in_b"), None),
                (circuit_voxel("out"), Some(data.powered)),
            ].into(),
            Block::NORGate(data) => [
                (circuit_voxel("in_a"), None),
                (circuit_voxel("in_b"), None),
                (circuit_voxel("out"), Some(data.powered)),
            ].into(),
            Block::XNORGate(data) => [
                (circuit_voxel("in_a"), None),
                (circuit_voxel("in_b"), None),
                (circuit_voxel("out"), Some(data.powered)),
            ].into(),
            Block::NOTGate(data) => [
                (circuit_voxel("in"), None),
                (circuit_voxel("out"), Some(data.powered)),
            ].into(),
            Block::Clock(data) => [
                (circuit_voxel("clock"), Some(data.powered)),
            ].into(),
            Block::Pulse(data) => [
                (circuit_voxel("pulse"), Some(data.powered)),
            ].into(),
            Block::Diode(data) => [
                (circuit_voxel("in"), None),
                (circuit_voxel("out"), Some(data.powered)),
            ].into(),
            Block::ToggleLatch(data) => [
                (circuit_voxel("in"), None),
                (circuit_voxel("out"), Some(data.powered)),
            ].into(),
            Block::PulseLatch(data) => [
                (circuit_voxel("in"), None),
                (circuit_voxel("out"), Some(data.powered)),
            ].into(),
            Block::MemoryLatch(data) => [
                (circuit_voxel("in_a"), None),
                (circuit_voxel("in_b"), None),
                (circuit_voxel("out"), Some(data.powered)),
            ].into(),
        }
    }

    /// Get this block's power state (returns value only if applicable)
    pub fn get_circuit_power(&self) -> Option<PowerState> {
        match self {
            Block::Wire(data) => Some(data.powered),
            Block::Toggle(data) => Some(data.powered),
            Block::ANDGate(data) => Some(data.powered),
            Block::ORGate(data) => Some(data.powered),
            Block::XORGate(data) => Some(data.powered),
            Block::NANDGate(data) => Some(data.powered),
            Block::NORGate(data) => Some(data.powered),
            Block::XNORGate(data) => Some(data.powered),
            Block::NOTGate(data) => Some(data.powered),
            Block::Clock(data) => Some(data.powered),
            Block::Pulse(data) => Some(data.powered),
            Block::Diode(data) => Some(data.powered),
            Block::ToggleLatch(data) => Some(data.powered),
            Block::PulseLatch(data) => Some(data.powered),
            Block::MemoryLatch(data) => Some(data.powered),
            _ => None
        }
    }

    /// Get all of the circuit voxels (global) belonging to this block
    pub fn get_global_circuit_voxels(&self, position: Coord, orientation: Orient) -> Vec<(VoxelID, Coord)> {
        self.get_global_structure(position, orientation)
            .iter()
            .filter(|(id, _)| is_circuit_voxel(id))
            .map(|(voxel_id, coord)| (voxel_id.clone(), *coord))
            .collect()
    }

    /// Return true if the block is a circuit block (i.e. has at least one circuit voxel)
    pub fn is_circuit_block(&self) -> bool {
        self.get_structure()
            .iter()
            .position(|(id, _)| is_circuit_voxel(id))
            .is_some()
    }

    /// Return the voxels that make up the block, adjusted to contain global coordinates
    pub fn get_global_structure(&self, position: Coord, orientation: Orient) -> HashMap<VoxelID, Coord> {
        self.get_structure()
            .into_iter()
            .map(|(id, mut coord)| {
                match orientation {
                    Orient::FORWARD => {
                        // Default orientation, do nothing
                    }
                    Orient::RIGHT => {
                        let t = coord.x;
                        coord.x = coord.z;
                        coord.z = t;
                    }
                    Orient::LEFT => {
                        let t = coord.x;
                        coord.x = -coord.z;
                        coord.z = -t;
                    }
                    Orient::BACKWARD => {
                        coord.z = -coord.z;
                        coord.x = -coord.x;
                    }
                    Orient::UPWARD => {
                        let t = coord.y;
                        coord.y = coord.z;
                        coord.z = t;
                    }
                    Orient::DOWNWARD => {
                        let t = coord.y;
                        coord.y = -coord.z;
                        coord.z = -t;
                    }
                }

                (id, coord + position)
            })
            .collect()
    }

    /// Return the voxels that make up the block
    ///
    /// Voxels whose names start with an exclamation are considered as part of the circuit
    pub fn get_structure(&self) -> HashMap<VoxelID, Coord> {
        match self {
            Block::Air => Default::default(),
            Block::Wire(_) => [(circuit_voxel("wire"), Coord::zero())].into(),
            Block::Block(_) => [("block".to_string(), Coord::zero())].into(),
            Block::Toggle(_) => [(circuit_voxel("toggle"), Coord::zero())].into(),
            Block::Pixel(_) => [(circuit_voxel("pixel"), Coord::zero())].into(),
            Block::ANDGate(_) => [
                (format!("solid-{}", 0), Coord::new(0, 0, 0)),
                (format!("solid-{}", 1), Coord::new(-1, 0, 1)),
                (format!("solid-{}", 2), Coord::new(1, 0, 1)),
                (format!("solid-{}", 3), Coord::new(-1, 0, 0)),
                (format!("solid-{}", 4), Coord::new(1, 0, 0)),
                (format!("solid-{}", 5), Coord::new(0, 0, -1)),
                (circuit_voxel("in_a"), Coord::new(-1, 0, -1)),
                (circuit_voxel("in_b"), Coord::new(1, 0, -1)),
                (circuit_voxel("out"), Coord::new(0, 0, 1)),
            ].into(),
            Block::ORGate(_) => [
                (format!("solid-{}", 0), Coord::new(0, 0, 0)),
                (format!("solid-{}", 1), Coord::new(-1, 0, 1)),
                (format!("solid-{}", 2), Coord::new(1, 0, 1)),
                (format!("solid-{}", 3), Coord::new(-1, 0, 0)),
                (format!("solid-{}", 4), Coord::new(1, 0, 0)),
                (format!("solid-{}", 5), Coord::new(0, 0, -1)),
                (circuit_voxel("in_a"), Coord::new(-1, 0, -1)),
                (circuit_voxel("in_b"), Coord::new(1, 0, -1)),
                (circuit_voxel("out"), Coord::new(0, 0, 1)),
            ].into(),
            Block::XORGate(_) => [
                (format!("solid-{}", 0), Coord::new(0, 0, 0)),
                (format!("solid-{}", 1), Coord::new(-1, 0, 1)),
                (format!("solid-{}", 2), Coord::new(1, 0, 1)),
                (format!("solid-{}", 3), Coord::new(-1, 0, 0)),
                (format!("solid-{}", 4), Coord::new(1, 0, 0)),
                (format!("solid-{}", 5), Coord::new(0, 0, -1)),
                (circuit_voxel("in_a"), Coord::new(-1, 0, -1)),
                (circuit_voxel("in_b"), Coord::new(1, 0, -1)),
                (circuit_voxel("out"), Coord::new(0, 0, 1)),
            ].into(),
            Block::NANDGate(_) => [
                (format!("solid-{}", 0), Coord::new(0, 0, 0)),
                (format!("solid-{}", 1), Coord::new(-1, 0, 1)),
                (format!("solid-{}", 2), Coord::new(1, 0, 1)),
                (format!("solid-{}", 3), Coord::new(-1, 0, 0)),
                (format!("solid-{}", 4), Coord::new(1, 0, 0)),
                (format!("solid-{}", 5), Coord::new(0, 0, -1)),
                (circuit_voxel("in_a"), Coord::new(-1, 0, -1)),
                (circuit_voxel("in_b"), Coord::new(1, 0, -1)),
                (circuit_voxel("out"), Coord::new(0, 0, 1)),
            ].into(),
            Block::NORGate(_) => [
                (format!("solid-{}", 0), Coord::new(0, 0, 0)),
                (format!("solid-{}", 1), Coord::new(-1, 0, 1)),
                (format!("solid-{}", 2), Coord::new(1, 0, 1)),
                (format!("solid-{}", 3), Coord::new(-1, 0, 0)),
                (format!("solid-{}", 4), Coord::new(1, 0, 0)),
                (format!("solid-{}", 5), Coord::new(0, 0, -1)),
                (circuit_voxel("in_a"), Coord::new(-1, 0, -1)),
                (circuit_voxel("in_b"), Coord::new(1, 0, -1)),
                (circuit_voxel("out"), Coord::new(0, 0, 1)),
            ].into(),
            Block::XNORGate(_) => [
                (format!("solid-{}", 0), Coord::new(0, 0, 0)),
                (format!("solid-{}", 1), Coord::new(-1, 0, 1)),
                (format!("solid-{}", 2), Coord::new(1, 0, 1)),
                (format!("solid-{}", 3), Coord::new(-1, 0, 0)),
                (format!("solid-{}", 4), Coord::new(1, 0, 0)),
                (format!("solid-{}", 5), Coord::new(0, 0, -1)),
                (circuit_voxel("in_a"), Coord::new(-1, 0, -1)),
                (circuit_voxel("in_b"), Coord::new(1, 0, -1)),
                (circuit_voxel("out"), Coord::new(0, 0, 1)),
            ].into(),
            Block::NOTGate(_) => [
                (circuit_voxel("in"), Coord::new(0, 0, 0)),
                (circuit_voxel("out"), Coord::new(0, 0, 1)),
            ].into(),
            Block::Clock(_) => [
                (circuit_voxel("clock"), Coord::zero()),
            ].into(),
            Block::Pulse(_) => [
                (circuit_voxel("pulse"), Coord::zero()),
            ].into(),
            Block::Diode(_) => [
                (circuit_voxel("in"), Coord::new(0, 0, 0)),
                (circuit_voxel("out"), Coord::new(0, 0, 1)),
            ].into(),
            Block::ToggleLatch(_) => [
                (circuit_voxel("in"), Coord::new(0, 0, 0)),
                (circuit_voxel("out"), Coord::new(0, 0, 1)),
            ].into(),
            Block::PulseLatch(_) => [
                (circuit_voxel("in"), Coord::new(0, 0, 0)),
                (circuit_voxel("out"), Coord::new(0, 0, 1)),
            ].into(),
            Block::MemoryLatch(_) => [
                (format!("solid-{}", 0), Coord::new(0, 0, 0)),
                (format!("solid-{}", 1), Coord::new(-1, 0, 1)),
                (format!("solid-{}", 2), Coord::new(1, 0, 1)),
                (format!("solid-{}", 3), Coord::new(-1, 0, 0)),
                (format!("solid-{}", 4), Coord::new(1, 0, 0)),
                (format!("solid-{}", 5), Coord::new(0, 0, -1)),
                (circuit_voxel("in_a"), Coord::new(-1, 0, -1)),
                (circuit_voxel("in_b"), Coord::new(1, 0, -1)),
                (circuit_voxel("out"), Coord::new(0, 0, 1)),
            ].into(),
        }
    }
}