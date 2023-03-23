use std::fmt::{Display, Formatter};

use serde::{Deserialize, Serialize};

pub type Color = (u8, u8, u8, u8);

#[derive(Clone, Debug, Serialize, Deserialize)]
pub enum Voxel
{
    Air,
    Wire {
        is_on: bool
    },
    Block {
        color: Color
    },
    Toggle {
        is_on: bool
    },
    Pixel {
        is_on: bool
    },
    ANDGate {
        is_on: bool
    },
    ORGate {
        is_on: bool
    },
    NOTGate {
        is_on: bool
    },
    Clock {
        frequency: u32,
        start_tick: u32,
    },
    Pulse {
        is_on: bool,
        pulse_ticks: u32,
    },
}

impl Display for Voxel
{
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}", self.get_name())
    }
}

impl Default for Voxel
{
    fn default() -> Self {
        Self::Air
    }
}

impl Voxel
{
    pub fn get_name(&self) -> &str {
        match self {
            Voxel::Air => "air",
            Voxel::Wire { .. } => "wire",
            Voxel::Block { .. } => "block",
            Voxel::Toggle { .. } => "toggle",
            Voxel::Pixel { .. } => "pixel",
            Voxel::ANDGate { .. } => "gate_and",
            Voxel::ORGate { .. } => "gate_or",
            Voxel::NOTGate { .. } => "gate_not",
            Voxel::Clock { .. } => "clock",
            Voxel::Pulse { .. } => "pulse",
        }
    }
}