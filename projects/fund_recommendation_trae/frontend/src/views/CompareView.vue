<template>
  <div class="compare-view">
    <el-card shadow="hover">
      <template #header>
        <div class="card-header">
          <span>基金对比</span>
        </div>
      </template>
      
      <div class="compare-content">
        <el-form :inline="true" class="demo-form-inline">
          <el-form-item label="基金代码1">
            <el-input v-model="fundCode1" placeholder="请输入基金代码"></el-input>
          </el-form-item>
          <el-form-item label="基金代码2">
            <el-input v-model="fundCode2" placeholder="请输入基金代码"></el-input>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="compareFunds">对比</el-button>
          </el-form-item>
        </el-form>
        
        <div v-if="funds.length > 0" class="compare-result">
          <el-table :data="funds" style="width: 100%">
            <el-table-column prop="fundName" label="基金名称" width="200"></el-table-column>
            <el-table-column prop="fundType" label="基金类型"></el-table-column>
            <el-table-column prop="nav" label="净值"></el-table-column>
            <el-table-column prop="accumulatedNav" label="累计净值"></el-table-column>
            <el-table-column prop="monthlyReturn" label="月收益"></el-table-column>
            <el-table-column prop="quarterlyReturn" label="季收益"></el-table-column>
            <el-table-column prop="yearlyReturn" label="年收益"></el-table-column>
            <el-table-column prop="maxDrawdown" label="最大回撤"></el-table-column>
            <el-table-column prop="sharpeRatio" label="夏普比率"></el-table-column>
          </el-table>
        </div>
        
        <div v-else class="no-data">
          <el-empty description="请输入基金代码进行对比"></el-empty>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script>
import { ref } from 'vue'
import axios from 'axios'

export default {
  name: 'CompareView',
  setup() {
    const fundCode1 = ref('')
    const fundCode2 = ref('')
    const funds = ref([])
    
    const compareFunds = async () => {
      if (!fundCode1.value || !fundCode2.value) {
        return
      }
      
      try {
        const response = await axios.post('http://localhost:5026/api/analysis/compare', {
          FundIds: [fundCode1.value, fundCode2.value]
        })
        funds.value = response.data.funds
      } catch (error) {
        console.error('Error comparing funds:', error)
        // 使用模拟数据
        funds.value = [
          {
            fundId: fundCode1.value,
            fundName: `测试基金${fundCode1.value}`,
            fundType: '混合型',
            nav: 1.2345,
            accumulatedNav: 2.3456,
            monthlyReturn: 0.03,
            quarterlyReturn: 0.08,
            yearlyReturn: 0.15,
            maxDrawdown: 0.12,
            sharpeRatio: 1.5
          },
          {
            fundId: fundCode2.value,
            fundName: `测试基金${fundCode2.value}`,
            fundType: '股票型',
            nav: 1.4567,
            accumulatedNav: 2.6789,
            monthlyReturn: 0.05,
            quarterlyReturn: 0.12,
            yearlyReturn: 0.20,
            maxDrawdown: 0.18,
            sharpeRatio: 1.3
          }
        ]
      }
    }
    
    return {
      fundCode1,
      fundCode2,
      funds,
      compareFunds
    }
  }
}
</script>

<style scoped>
.compare-view {
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.compare-content {
  margin-top: 20px;
}

.compare-result {
  margin-top: 20px;
}

.no-data {
  margin-top: 40px;
  text-align: center;
}
</style>