use kd_tree::KdTree3;
use petgraph::graph::DiGraph;
use serde::{Deserialize, Serialize};

use crate::voxel::Voxel;

pub type Resistance = u32;

#[derive(Clone, Debug, Default, Serialize, Deserialize)]
pub struct Scene
{
    circuit: DiGraph<Voxel, Resistance>,
    space: KdTree3<Voxel>,
}

impl Scene
{}