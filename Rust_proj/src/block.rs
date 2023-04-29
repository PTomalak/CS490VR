use std::collections::HashMap;
use std::fmt::{Display, Formatter};

use cgmath::Zero;
use serde::{Deserialize, Serialize};

use crate::grid::Coord;

pub type VoxelID = String;
pub type PowerState = bool;
pub type Color = (u8, u8, u8, u8);

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
    FORWARD,
    RIGHT,
    LEFT,
    BACKWARD,
    UPWARD,
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
pub struct VoxelPixel
{
    pub on: bool,
}

#[derive(Clone, Debug, Default, PartialEq, Serialize, Deserialize)]
pub struct VoxelBlock
{
    pub color: Color,
}

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
    Air,
    Wire(VoxelPowered),
    Block(VoxelBlock),
    Toggle(VoxelPowered),
    Pixel(VoxelPixel),
    ANDGate(VoxelPowered),
    ORGate(VoxelPowered),
    NOTGate(VoxelPowered),
    Clock(VoxelClock),
    Pulse(VoxelPulse),
}

impl Display for Block
{
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}", self.get_name())
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
    /// Get this block's power state (returns value only if applicable)
    pub fn get_circuit_power(&self) -> Option<PowerState> {
        match self {
            Block::Wire(data) => Some(data.powered),
            Block::Toggle(data) => Some(data.powered),
            Block::ANDGate(data) => Some(data.powered),
            Block::ORGate(data) => Some(data.powered),
            Block::NOTGate(data) => Some(data.powered),
            Block::Clock(data) => Some(data.powered),
            Block::Pulse(data) => Some(data.powered),
            _ => None
        }
    }

    /// Get this block's power state (excludes wires, returns value only if applicable)
    pub fn get_non_wire_power(&self) -> Option<PowerState> {
        match self {
            Block::Toggle(data) => Some(data.powered),
            Block::ANDGate(data) => Some(data.powered),
            Block::ORGate(data) => Some(data.powered),
            Block::NOTGate(data) => Some(data.powered),
            Block::Clock(data) => Some(data.powered),
            Block::Pulse(data) => Some(data.powered),
            _ => None
        }
    }

    /// Get all of the circuit voxels belonging to this block
    pub fn get_circuit_voxels(&self) -> Vec<(VoxelID, Coord)> {
        self.get_structure()
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

    /// Return the voxels that make up the block
    /// Voxels whose names start with an exclamation are considered as part of the circuit
    pub fn get_structure(&self) -> HashMap<VoxelID, Coord> {
        match self {
            Block::Air => Default::default(),
            Block::Wire(_) => [(circuit_voxel("wire"), Coord::zero())].into(),
            Block::Block(_) => [("block".to_string(), Coord::zero())].into(),
            Block::Toggle(_) => [(circuit_voxel("toggle"), Coord::zero())].into(),
            Block::Pixel(_) => [(circuit_voxel("pixel"), Coord::zero())].into(),
            Block::ANDGate(_) => [
                (circuit_voxel("in_a"), Coord::new(-1, 1, 0)),
                (circuit_voxel("in_b"), Coord::new(-1, -1, 0)),
                (circuit_voxel("out"), Coord::new(1, 0, 0)),
            ].into(),
            Block::ORGate(_) => [
                (circuit_voxel("in_a"), Coord::new(-1, 1, 0)),
                (circuit_voxel("in_b"), Coord::new(-1, -1, 0)),
                (circuit_voxel("out"), Coord::new(1, 0, 0)),
            ].into(),
            Block::NOTGate(_) => [
                (circuit_voxel("in_a"), Coord::new(-1, 0, 0)),
                (circuit_voxel("out"), Coord::new(0, 0, 0)),
            ].into(),
            Block::Clock(_) => [
                (circuit_voxel("clock"), Coord::zero()),
            ].into(),
            Block::Pulse(_) => [
                (circuit_voxel("pulse"), Coord::zero()),
            ].into(),
        }
    }

    pub fn get_name(&self) -> &str {
        match self {
            Block::Air => "air",
            Block::Wire(_) => "wire",
            Block::Block(_) => "block",
            Block::Toggle(_) => "toggle",
            Block::Pixel(_) => "pixel",
            Block::ANDGate(_) => "gate_and",
            Block::ORGate(_) => "gate_or",
            Block::NOTGate(_) => "gate_not",
            Block::Clock(_) => "clock",
            Block::Pulse(_) => "pulse",
        }
    }
}